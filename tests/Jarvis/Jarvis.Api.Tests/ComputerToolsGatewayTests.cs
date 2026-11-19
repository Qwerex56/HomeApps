using HomeApps.BuildingBlocks.Testing;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Tests;

/// <summary>
/// H-4c items 6 and 10: what is on a person's own computer is kept from a household satellite, and an external MCP client
/// cannot make a computer do something that asks for confirmation. The Devices source is a fake with the tools the test sets.
/// </summary>
[Collection(nameof(JarvisCollection))]
public sealed class ComputerToolsGatewayTests : IDisposable
{
    private const string NotOnSatellite = "This tool is on a person's own computer and cannot be used from a satellite.";
    private const string NeedsConfirmation = "This action needs the person's confirmation. Use JARVIS in the HomeApps app.";

    private readonly JarvisFactory _factory;
    private readonly Guid _person = Guid.NewGuid();
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _device = Guid.NewGuid();

    public ComputerToolsGatewayTests(JarvisFactory factory)
    {
        _factory = factory;
        _factory.DevicesSource.Reset();
        _factory.DevicesSource.Tools =
        [
            FakeToolSet.Computer("mac_open_app", readOnly: false, destructive: true),
            FakeToolSet.Computer("mac_peek", readOnly: true),
            FakeToolSet.Computer("mac_quiet", readOnly: false, destructive: false),
            FakeToolSet.Computer("mac_bare", withAnnotations: false),
            FakeToolSet.Household("fotel_say", _household, readOnly: false, destructive: false),
            FakeToolSet.Household("fotel_ask", _household, readOnly: false, destructive: true),
        ];
    }

    public void Dispose() => _factory.DevicesSource.Reset();

    private static string[] Computer => ["device_mac_open_app", "device_mac_peek", "device_mac_quiet", "device_mac_bare"];

    /// <summary>An external MCP client: the person's own token, no run.</summary>
    private async Task<McpClient> ExternalAsync() =>
        await McpTestClient.ConnectAsync(_factory.CreateClient(TestJwt.CreateToken(_person)), "jarvis/mcp");

    [Fact]
    public async Task A_satellite_run_is_not_offered_the_computer_tools_but_keeps_the_household_ones()
    {
        IReadOnlyList<string> offered = [];
        _factory.Brain.Script = async mcp =>
        {
            offered = [.. (await mcp.ListToolsAsync()).Select(t => t.Name)];
            return "Gotowe.";
        };
        using var socket = await SatelliteSocket.ConnectAsync(_factory, _device, _household);

        await SatelliteSocket.SayAsync(socket, "otwórz aplikację");

        Assert.Contains("device_fotel_say", offered);
        Assert.Contains("device_fotel_ask", offered);
        Assert.All(Computer, name => Assert.DoesNotContain(name, offered));
    }

    [Fact]
    public async Task A_satellite_run_calling_a_computer_tool_gets_a_tool_error_and_the_source_is_not_called()
    {
        var answers = new List<(string Text, bool IsError)>();
        _factory.Brain.Script = async mcp =>
        {
            foreach (var name in Computer)
            {
                answers.Add(await mcp.CallTextAsync(name, new Dictionary<string, object?>()));
            }

            return "Gotowe.";
        };
        using var socket = await SatelliteSocket.ConnectAsync(_factory, _device, _household);

        await SatelliteSocket.SayAsync(socket, "otwórz aplikację");

        Assert.Equal(4, answers.Count);
        Assert.All(answers, a => Assert.Equal((NotOnSatellite, true), a));
        Assert.Empty(_factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task A_satellite_run_still_reaches_a_household_tool()
    {
        (string Text, bool IsError) answer = default;
        _factory.Brain.Script = async mcp =>
        {
            answer = await mcp.CallTextAsync("device_fotel_say", new Dictionary<string, object?>());
            return "Gotowe.";
        };
        using var socket = await SatelliteSocket.ConnectAsync(_factory, _device, _household);

        await SatelliteSocket.SayAsync(socket, "powiedz coś");

        Assert.False(answer.IsError);
        Assert.Equal(["fotel_say"], _factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task A_run_that_is_not_from_a_satellite_keeps_the_computer_tools_in_the_list()
    {
        IReadOnlyList<string> offered = [];
        (string Text, bool IsError) peek = default;
        _factory.Brain.Script = async mcp =>
        {
            offered = [.. (await mcp.ListToolsAsync()).Select(t => t.Name)];
            peek = await mcp.CallTextAsync("device_mac_peek", new Dictionary<string, object?>());
            return "Gotowe.";
        };

        await AssistantTests.AskAsync(_factory, _person, "co jest na ekranie?");

        Assert.All(Computer, name => Assert.Contains(name, offered));
        Assert.False(peek.IsError);
        Assert.Contains("mac_peek", _factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task An_external_client_sees_the_same_list_as_before_computer_tools_included()
    {
        await using var mcp = await ExternalAsync();

        var names = (await mcp.ListToolsAsync()).Select(t => t.Name).ToList();

        Assert.All(Computer, name => Assert.Contains(name, names));
        Assert.Contains("device_fotel_say", names);
    }

    [Fact]
    public async Task An_external_client_cannot_run_a_changing_computer_tool_that_asks_and_the_source_is_not_called()
    {
        await using var mcp = await ExternalAsync();

        var asks = await mcp.CallTextAsync("device_mac_open_app", new Dictionary<string, object?>());
        var bare = await mcp.CallTextAsync("device_mac_bare", new Dictionary<string, object?>());

        Assert.Equal((NeedsConfirmation, true), asks);
        Assert.Equal((NeedsConfirmation, true), bare);
        Assert.Empty(_factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task An_external_client_runs_a_read_only_computer_tool_and_one_with_nie_pytaj()
    {
        await using var mcp = await ExternalAsync();

        var peek = await mcp.CallTextAsync("device_mac_peek", new Dictionary<string, object?>());
        var quiet = await mcp.CallTextAsync("device_mac_quiet", new Dictionary<string, object?>());

        Assert.False(peek.IsError);
        Assert.False(quiet.IsError);
        Assert.Equal(["mac_peek", "mac_quiet"], _factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task An_external_client_calling_household_tools_is_unchanged_whether_they_ask_or_not()
    {
        await using var mcp = await ExternalAsync();

        var say = await mcp.CallTextAsync("device_fotel_say", new Dictionary<string, object?>());
        var ask = await mcp.CallTextAsync("device_fotel_ask", new Dictionary<string, object?>());

        Assert.False(say.IsError);
        Assert.False(ask.IsError);
        Assert.Equal(["fotel_say", "fotel_ask"], _factory.DevicesSource.Calls);
    }
}

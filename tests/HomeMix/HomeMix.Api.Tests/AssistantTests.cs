using System.Net;
using System.Net.Http.Json;
using HomeApps.BuildingBlocks.Testing;

namespace HomeMix.Api.Tests;

/// <summary>
/// jarvis-v1.md, 10a: an assistant acting for a person reads add-ons but never adds, installs, updates or removes
/// them; until stage A6 the MCP server JARVIS lists has no tools.
/// </summary>
public sealed class AssistantTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    [Fact]
    public async Task An_assistant_reads_but_never_changes_addons()
    {
        var admin = Guid.NewGuid();
        var household = api.Household(admin);
        using var admins = api.SignedIn(admin);
        var sha = await admins.PackageAsync(AddonZip.Pomodoro(version: "4.0.0"));
        var id = await admins.InstalledInAsync(household, AddonZip.Pomodoro(version: "4.0.1"));
        using var assistant = api.Assistant(admin);

        using var list = await assistant.GetAsync($"/homemix/api/households/{household}/installations");
        using var one = await assistant.GetAsync($"/homemix/api/installations/{id}");
        using var upload = await assistant.UploadAsync(AddonZip.Pomodoro(version: "4.0.2"));
        using var github = await assistant.PostAsJsonAsync("/homemix/api/packages/github", new { repository = "https://github.com/a/b" });
        using var install = await assistant.PostAsJsonAsync("/homemix/api/me/installations", new { sha256 = sha, consent = Requests.Household });
        using var update = await assistant.PutAsJsonAsync($"/homemix/api/installations/{id}", new { sha256 = sha });
        using var remove = await assistant.DeleteAsync($"/homemix/api/installations/{id}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, one.StatusCode);
        foreach (var refused in new[] { upload, github, install, update, remove })
        {
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
            Assert.Equal("assistant.not_allowed", await refused.CodeAsync());
        }
    }

    [Fact]
    public async Task The_mcp_server_answers_with_no_tools_yet()
    {
        await using var mcp = await McpTestClient.ConnectAsync(api.Assistant(Guid.NewGuid()));

        var tools = await mcp.ListToolsAsync();

        Assert.Empty(tools);
        Assert.Equal("homeapps-homemix", mcp.ServerInfo!.Name);
    }
}

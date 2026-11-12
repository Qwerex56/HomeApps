using System.Net;
using System.Net.Http.Json;
using HomeApps.BuildingBlocks.Api.Assistant;
using HomeApps.BuildingBlocks.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMix.Api.Tests;

/// <summary>
/// jarvis-v1.md, 10a: an assistant acting for a person reads add-ons but never adds, installs, updates or removes
/// them; it may change the add-ons' own data. Until stage A6 the MCP server JARVIS lists has no tools.
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
    public async Task An_assistant_changes_the_addons_own_data()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(api.Household(admin), AddonZip.Pomodoro(version: "4.1.0"));
        using var assistant = api.Assistant(admin);
        var url = $"/homemix/api/installations/{id}/data/sessions/s1";

        using var put = await assistant.PutAsJsonAsync(url, new { value = new { minutes = 25 } });
        using var delete = await assistant.DeleteAsync(url);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    /// <summary>
    /// Every endpoint that changes something, and whether an assistant may call it. A new one fails here until it is
    /// added on purpose: core changes are refused by default, only the add-ons' data and MCP are allowed.
    /// </summary>
    [Fact]
    public void Which_changes_an_assistant_may_make_is_pinned()
    {
        var changes = api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IHttpMethodMetadata>() is not null)
            .SelectMany(e => e.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods
                .Where(m => m is not ("GET" or "HEAD" or "OPTIONS"))
                .Select(m => $"{m} {e.RoutePattern.RawText} {e.Metadata.GetMetadata<AssistantAccessMetadata>()?.Access ?? AssistantAccess.Default}"))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
        [
            "DELETE /homemix/api/installations/{id:guid} Default",
            "DELETE /homemix/api/installations/{id:guid}/data/{collection}/{key} Allow",
            "POST /homemix/api/households/{householdId:guid}/installations Default",
            "POST /homemix/api/me/installations Default",
            "POST /homemix/api/packages/ Default",
            "POST /homemix/api/packages/github Default",
            "POST /mcp/ Allow",
            "PUT /homemix/api/installations/{id:guid} Default",
            "PUT /homemix/api/installations/{id:guid}/data/{collection}/{key} Allow",
        ], changes);
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

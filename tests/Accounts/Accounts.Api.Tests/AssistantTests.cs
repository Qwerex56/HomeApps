using System.Net;
using System.Net.Http.Json;
using HomeApps.BuildingBlocks.Testing;

namespace Accounts.Api.Tests;

/// <summary>jarvis-v1.md, JV13: an assistant acting for a person reads the account but never changes it.</summary>
public sealed class AssistantTests(AccountsApiFactory api) : IClassFixture<AccountsApiFactory>
{
    [Fact]
    public async Task An_assistant_reads_but_never_changes_the_account()
    {
        var id = await api.RegisterAsync(displayName: "Ola");
        using var assistant = api.ClientWith(api.AssistantToken(id));

        using var me = await assistant.GetAsync("/users/me");
        using var profile = await assistant.PutAsJsonAsync("/users/me/profile", AccountsApiFactory.Profile("Ktoś Inny"));

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, profile.StatusCode);
        Assert.Contains("assistant.not_allowed", await profile.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_profile_tool_answers_in_plain_text()
    {
        var id = await api.RegisterAsync(displayName: "Ola");
        await using var mcp = await McpTestClient.ConnectAsync(api.ClientWith(api.AssistantToken(id)));

        var tools = await mcp.ListToolsAsync();
        var profile = await mcp.CallTextAsync("my_profile");

        Assert.Equal("my_profile", Assert.Single(tools).Name);
        Assert.False(profile.IsError);
        Assert.Contains("Name: Ola", profile.Text, StringComparison.Ordinal);
    }
}

using System.Net;
using System.Net.Http.Json;
using HomeApps.BuildingBlocks.Testing;

namespace Households.Api.Tests;

/// <summary>
/// jarvis-v1.md, JV13: an assistant acting for a person only reads households; every change answers 403
/// <c>assistant.not_allowed</c>. The MCP tools read through the same queries.
/// </summary>
public sealed class AssistantTests(HouseholdsApiFactory api) : IClassFixture<HouseholdsApiFactory>
{
    [Fact]
    public async Task An_assistant_reads_but_never_changes_households()
    {
        var owner = await api.UserAsync();
        var household = await api.HouseholdAsync(owner, "Dom Kowalskich");
        using var assistant = api.ClientWith(api.AssistantToken(owner));

        using var list = await assistant.GetAsync("/households");
        using var create = await assistant.PostAsJsonAsync("/households", new { name = "Nowy" });
        using var rename = await assistant.PutAsJsonAsync($"/households/{household}", new { name = "Inny" });
        using var archive = await assistant.PostAsync($"/households/{household}/archive", null);
        using var invite = await assistant.PostAsJsonAsync($"/households/{household}/invites", new { role = "Member" });

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        foreach (var refused in new[] { create, rename, archive, invite })
        {
            Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
            Assert.Equal("assistant.not_allowed", await refused.CodeAsync());
        }

        using var owners = api.SignedIn(owner);
        var detail = await owners.GetFromJsonAsync<System.Text.Json.JsonElement>($"/households/{household}");
        Assert.Equal("Dom Kowalskich", detail.GetProperty("name").GetString());
    }

    [Fact]
    public async Task The_person_is_not_affected()
    {
        using var person = api.SignedIn(await api.UserAsync());

        using var create = await person.PostAsJsonAsync("/households", new { name = "Dom" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    [Fact]
    public async Task Mcp_tools_read_households_and_members_by_name()
    {
        var owner = await api.UserAsync("Adam");
        var household = await api.HouseholdAsync(owner, "Dom Nowaków");
        await api.MemberAsync(household, owner, displayName: "Ewa");
        await using var mcp = await McpTestClient.ConnectAsync(api.ClientWith(api.AssistantToken(owner)));

        var tools = await mcp.ListToolsAsync();
        var mine = await mcp.CallTextAsync("my_households");
        var members = await mcp.CallTextAsync("household_members", new Dictionary<string, object?> { ["household"] = "dom nowaków" });
        var unknown = await mcp.CallTextAsync("household_members", new Dictionary<string, object?> { ["household"] = "Zamek" });

        Assert.Equal(["household_members", "my_households"], tools.Select(t => t.Name).Order(StringComparer.Ordinal));
        Assert.All(tools, t => Assert.True(t.ProtocolTool.Annotations!.ReadOnlyHint));
        Assert.Contains($"Dom Nowaków (id {household}), role: Owner, 2 members", mine.Text, StringComparison.Ordinal);
        Assert.False(members.IsError);
        Assert.Contains("- Adam: Owner", members.Text, StringComparison.Ordinal);
        Assert.Contains("- Ewa: Member", members.Text, StringComparison.Ordinal);
        Assert.True(unknown.IsError);
        Assert.Contains("household.not_found", unknown.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_guest_gets_names_only_through_the_tool()
    {
        var owner = await api.UserAsync("Adam");
        var household = await api.HouseholdAsync(owner, "Dom");
        var guest = await api.MemberAsync(household, owner, role: "Guest", displayName: "Gość");
        await using var mcp = await McpTestClient.ConnectAsync(api.ClientWith(api.AssistantToken(guest)));

        var members = await mcp.CallTextAsync("household_members", new Dictionary<string, object?> { ["household"] = household.ToString() });

        Assert.False(members.IsError);
        Assert.Contains("- Adam", members.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Owner", members.Text, StringComparison.Ordinal);
    }
}

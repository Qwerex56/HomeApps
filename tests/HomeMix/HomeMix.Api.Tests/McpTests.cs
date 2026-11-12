using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Households;
using ModelContextProtocol.Client;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "JARVIS": declared tools per person, through the same rules as the bridge.</summary>
public sealed class McpTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private static readonly string[] AdminTools =
        ["notes_add_note", "notes_forget", "notes_notes", "notes_secret", "notes_star", "notes_summary"];

    private static readonly string[] GuestTools = ["notes_notes", "notes_summary"];

    [Fact]
    public async Task Each_person_sees_the_tools_their_role_allows()
    {
        var (admin, guest) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, guest, HouseholdRole.Guest);
        using var admins = api.SignedIn(admin);
        await admins.InstalledInAsync(household, AddonZip.Notes(), Requests.NotesForJarvis);

        var adminTools = await ToolsAsync(api.Assistant(admin));
        var guestTools = await ToolsAsync(api.Assistant(guest));
        var strangerTools = await ToolsAsync(api.Assistant(Guid.NewGuid()));

        Assert.Equal(AdminTools, adminTools.Select(t => t.Name).Order(StringComparer.Ordinal));
        Assert.Equal(GuestTools, guestTools.Select(t => t.Name).Order(StringComparer.Ordinal));
        Assert.Empty(strangerTools);
        var byName = adminTools.ToDictionary(t => t.Name);
        Assert.True(byName["notes_forget"].ProtocolTool.Annotations!.DestructiveHint);
        Assert.False(byName["notes_add_note"].ProtocolTool.Annotations!.DestructiveHint);
        Assert.True(byName["notes_notes"].ProtocolTool.Annotations!.ReadOnlyHint);
        Assert.True(byName["notes_summary"].ProtocolTool.Annotations!.ReadOnlyHint);
        Assert.Contains("\"text\"", byName["notes_add_note"].JsonSchema.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Without_the_installers_consent_JARVIS_gets_nothing()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        await admins.InstalledInAsync(api.Household(admin), AddonZip.Notes(version: "1.0.1"), Requests.NotesWithoutJarvis);

        Assert.Empty(await ToolsAsync(api.Assistant(admin)));
    }

    [Fact]
    public async Task Tools_add_list_update_and_delete_entries()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        await admins.InstalledInAsync(api.Household(admin), AddonZip.Notes(version: "1.0.2"), Requests.NotesForJarvis);
        await using var mcp = await McpTestClient.ConnectAsync(api.Assistant(admin));

        var added = await mcp.CallTextAsync("notes_add_note", new Dictionary<string, object?> { ["text"] = "Mleko" });
        var listed = await mcp.CallTextAsync("notes_notes");
        var key = listed.Text.Split('\n')[1].TrimStart('-', ' ').Split(':')[0];
        var starred = await mcp.CallTextAsync("notes_star", new Dictionary<string, object?> { ["key"] = key, ["stars"] = 5 });
        var summary = await mcp.CallTextAsync("notes_summary");
        var forgotten = await mcp.CallTextAsync("notes_forget", new Dictionary<string, object?> { ["key"] = key });
        var empty = await mcp.CallTextAsync("notes_notes");

        Assert.Equal(("Noted: Mleko.", false), added);
        Assert.StartsWith("1 in notes:", listed.Text, StringComparison.Ordinal);
        Assert.Contains("text: Mleko, stars: 1", listed.Text, StringComparison.Ordinal);
        Assert.Equal(($"Updated {key}.", false), starred);
        Assert.Contains("text: Mleko, stars: 5", summary.Text, StringComparison.Ordinal);
        Assert.Equal(($"Deleted {key}.", false), forgotten);
        Assert.Equal("nothing in notes yet.", empty.Text);
    }

    [Fact]
    public async Task Arguments_are_checked_before_anything_is_written()
    {
        var admin = Guid.NewGuid();
        using var admins = api.SignedIn(admin);
        await admins.InstalledInAsync(api.Household(admin), AddonZip.Notes(version: "1.0.3"), Requests.NotesForJarvis);
        await using var mcp = await McpTestClient.ConnectAsync(api.Assistant(admin));

        var missing = await mcp.CallTextAsync("notes_add_note");
        var wrongType = await mcp.CallTextAsync("notes_add_note", new Dictionary<string, object?> { ["text"] = "a", ["stars"] = "many" });
        var unknown = await mcp.CallTextAsync("notes_add_note", new Dictionary<string, object?> { ["text"] = "a", ["color"] = "red" });
        var noKey = await mcp.CallTextAsync("notes_forget");
        var noTool = await mcp.CallTextAsync("notes_fly");
        var stillEmpty = await mcp.CallTextAsync("notes_notes");

        Assert.True(missing.IsError);
        Assert.Contains("text is required", missing.Text, StringComparison.Ordinal);
        Assert.Contains("stars must be a integer", wrongType.Text, StringComparison.Ordinal);
        Assert.Contains("Unknown argument color", unknown.Text, StringComparison.Ordinal);
        Assert.Contains("key is required", noKey.Text, StringComparison.Ordinal);
        Assert.Contains("homemix.tool_not_found", noTool.Text, StringComparison.Ordinal);
        Assert.Equal("nothing in notes yet.", stillEmpty.Text);
    }

    [Fact]
    public async Task In_two_places_a_tool_asks_which_household()
    {
        var admin = Guid.NewGuid();
        var (home, cottage) = (api.Household(admin), api.Household(admin));
        api.Households.Name(home, "Dom");
        api.Households.Name(cottage, "Działka");
        using var admins = api.SignedIn(admin);
        await admins.InstalledInAsync(home, AddonZip.Notes(version: "1.0.4"), Requests.NotesForJarvis);
        await admins.InstalledInAsync(cottage, AddonZip.Notes(version: "1.0.4"), Requests.NotesForJarvis);
        await using var mcp = await McpTestClient.ConnectAsync(api.Assistant(admin));

        var tools = await mcp.ListToolsAsync();
        var which = await mcp.CallTextAsync("notes_add_note", new Dictionary<string, object?> { ["text"] = "Grabie" });
        var added = await mcp.CallTextAsync("notes_add_note",
            new Dictionary<string, object?> { ["text"] = "Grabie", ["household"] = "działka" });
        var atHome = await mcp.CallTextAsync("notes_notes", new Dictionary<string, object?> { ["household"] = "Dom" });

        Assert.Contains("\"household\"", tools.Single(t => t.Name == "notes_add_note").JsonSchema.GetRawText(), StringComparison.Ordinal);
        Assert.True(which.IsError);
        Assert.Contains("Which household?", which.Text, StringComparison.Ordinal);
        Assert.Equal(("Noted: Grabie.", false), added);
        Assert.Equal("nothing in notes yet.", atHome.Text);
    }

    private static async Task<IList<McpClientTool>> ToolsAsync(HttpClient http)
    {
        using (http)
        {
            await using var mcp = await McpTestClient.ConnectAsync(http);
            return await mcp.ListToolsAsync();
        }
    }
}

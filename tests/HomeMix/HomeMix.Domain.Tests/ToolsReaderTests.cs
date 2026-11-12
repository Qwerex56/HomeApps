using HomeMix.Domain.Data;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Packages;
using HomeMix.Domain.Tools;

namespace HomeMix.Domain.Tests;

/// <summary>docs/homemix.md, "JARVIS": tools and summary in homemix.json.</summary>
public sealed class ToolsReaderTests
{
    private const string Permissions = """ "permissions": [{ "id": "stats", "minRole": "Admin" }]""";

    [Fact]
    public void Declared_tools_and_summary_are_read()
    {
        var manifest = AddonManifestParser.Parse(Manifests.Valid(capabilities: """["storage.household", "storage.user"]""", extra: $$"""
            "jarvis": true, {{Permissions}},
            "tools": [
              { "name": "start", "description": "Starts a focus.", "collection": "sessions", "action": "add", "scope": "user",
                "fields": { "minutes": { "type": "integer", "default": 25 } }, "answer": "Started {minutes} min." },
              { "name": "stats", "description": "Shows stats.", "collection": "stats", "action": "list", "permission": "stats" }
            ],
            "summary": { "collection": "stats", "description": "Today's focus." }
            """)).Value;

        Assert.Collection(manifest.Tools,
            start =>
            {
                Assert.Equal(ToolAction.Add, start.Action);
                Assert.Equal(DataScope.User, start.Scope);
                Assert.True(start.Changes);
                Assert.Equal(25, Assert.Single(start.Fields).Default!.Value.GetInt32());
            },
            stats =>
            {
                Assert.Equal(DataScope.Household, stats.Scope);
                Assert.Equal("stats", stats.Permission);
                Assert.False(stats.Changes);
            });
        Assert.Equal("stats", manifest.Summary!.Collection);
    }

    [Theory]
    [InlineData(""" "jarvis": false, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "list" }]""", "set jarvis to true")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "summary", "description": "d", "collection": "c", "action": "list" }]""", ".name must be")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "collection": "c", "action": "list" }]""", ".description is required")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "fly" }]""", ".action must be")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "list", "scope": "user" }]""", "needs the capability storage.user")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "list", "fields": { "n": { "type": "string" } } }]""", "only add, update and script")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "add", "fields": { "key": { "type": "string" } } }]""", "not key or household")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "add", "fields": { "n": { "type": "date" } } }]""", ".type must be")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "add", "fields": { "n": { "type": "integer", "default": "x" } } }]""", ".default must be a integer")]
    [InlineData(""" "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "list", "permission": "nope" }]""", "is not in permissions")]
    public void A_wrong_tool_is_a_manifest_problem(string extra, string problem)
    {
        var result = AddonManifestParser.Parse(Manifests.Valid(extra: extra));

        Assert.True(result.IsFailed);
        Assert.Contains(result.Errors, e => e.Message.Contains(problem, StringComparison.Ordinal));
    }

    [Fact]
    public void An_exposed_name_fits_in_64_characters()
    {
        var result = AddonManifestParser.Parse(Manifests.Valid(id: new string('a', 30), extra: """
            "jarvis": true, "tools": [{ "name": "abcdefghijabcdefghijabcdefghijabcde", "description": "d", "collection": "c", "action": "list" }]
            """));

        Assert.Contains(result.Errors, e => e.Message.Contains("is longer than 64", StringComparison.Ordinal));
    }

    [Fact]
    public void A_script_tool_needs_its_file_in_the_package()
    {
        var manifest = AddonManifestParser.Parse(Manifests.Valid(extra: """
            "jarvis": true, "tools": [{ "name": "forecast", "description": "d", "action": "script", "readOnly": true,
              "fields": { "day": { "type": "integer", "default": 1 } } }]
            """)).Value;

        var without = PackageRules.Check([new PackageFile("index.html", 10)], manifest);
        var with = PackageRules.Check([new PackageFile("index.html", 10), new PackageFile("tools/forecast.js", 10)], manifest);

        var tool = Assert.Single(manifest.Tools);
        Assert.Equal(ToolAction.Script, tool.Action);
        Assert.False(tool.Changes);
        Assert.Equal("tools/forecast.js", tool.ScriptPath);
        Assert.Contains(without, p => p.Contains("tools/forecast.js", StringComparison.Ordinal));
        Assert.Empty(with);
    }

    [Fact]
    public void ReadOnly_is_only_for_scripts()
    {
        var result = AddonManifestParser.Parse(Manifests.Valid(extra: """
            "jarvis": true, "tools": [{ "name": "a", "description": "d", "collection": "c", "action": "list", "readOnly": true }]
            """));

        Assert.Contains(result.Errors, e => e.Message.Contains("readOnly is for script tools", StringComparison.Ordinal));
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using HomeApps.Contracts.Households;
using HomeApps.Plugins.Manifest;

namespace HomeApps.Plugins.Tests;

public sealed class ManifestTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public void A_complete_manifest_is_valid() => Assert.Empty(PluginManifestValidator.Validate(TestManifests.Valid));

    [Theory]
    [InlineData("Todo")]
    [InlineData("t")]
    [InlineData("1todo")]
    [InlineData("to_do")]
    [InlineData("households")]
    [InlineData("auth")]
    public void Bad_or_reserved_ids_are_refused(string id) =>
        Assert.NotEmpty(PluginManifestValidator.Validate(TestManifests.Valid with { Id = id }));

    [Fact]
    public void Household_data_needs_someone_to_switch_the_plugin_on()
    {
        var problems = PluginManifestValidator.Validate(TestManifests.Valid with { Enablement = null });

        Assert.Contains(problems, p => p.Contains("Enablement", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_data_alone_needs_no_switch() =>
        Assert.Empty(PluginManifestValidator.Validate(TestManifests.Valid with { Scopes = [DataScope.Application], Enablement = null }));

    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        var manifest = TestManifests.Valid with
        {
            Version = "one",
            Name = new LocalizedText("Zadania", ""),
            Permissions = [new PluginPermission("tasks.read", HouseholdRole.Guest), new PluginPermission("tasks.read", HouseholdRole.Member), new PluginPermission("Tasks Write", HouseholdRole.Member)],
            Display = [new DisplayScreen("today", new LocalizedText("a", "b"), "/households/x", 10)],
            Mcp = new PluginMcp([]),
        };

        var problems = PluginManifestValidator.Validate(manifest);

        Assert.Equal(7, problems.Count);
        var exception = Assert.Throws<InvalidOperationException>(() => PluginManifestValidator.EnsureValid(manifest));
        Assert.Contains("tasks.read", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_document_puts_everything_under_the_plugins_prefix()
    {
        var document = PluginManifestDocument.From(TestManifests.Valid);

        Assert.Equal(PluginManifestDocument.CurrentVersion, document.ManifestVersion);
        Assert.Equal("/plugins/todo/api", document.Endpoints.Api);
        Assert.Equal("/plugins/todo/openapi/v1.json", document.Endpoints.OpenApi);
        Assert.Equal("/plugins/todo/mcp", document.Endpoints.Mcp);
        Assert.Equal("/plugins/todo/health/ready", document.Endpoints.Health);
        Assert.Equal("/plugins/todo/ui/entry.js", document.Ui!.Entry);
        Assert.Equal("/plugins/todo/api/households/{householdId}/display/today", Assert.Single(document.Display).Path);
        Assert.Equal("/plugins/todo/api/households/{householdId}/enablement", document.Enablement!.Path);
        Assert.Equal("HomeApps.Contracts.Households.MemberLeft", Assert.Single(document.Events.Consumes));
    }

    [Fact]
    public void Notification_categories_are_checked_and_listed()
    {
        var manifest = TestManifests.Valid with
        {
            Notifications =
            [
                new PluginNotificationCategory("reminder", new LocalizedText("Przypomnienia", "Reminders")),
                new PluginNotificationCategory("reminder", new LocalizedText("Przypomnienia", "")),
                new PluginNotificationCategory("Due Soon", new LocalizedText("a", "b")),
            ],
        };

        Assert.Equal(3, PluginManifestValidator.Validate(manifest).Count);
        var valid = TestManifests.Valid with { Notifications = [new PluginNotificationCategory("reminder", new LocalizedText("Przypomnienia", "Reminders"))] };
        Assert.Equal("reminder", Assert.Single(PluginManifestDocument.From(valid).Notifications).Id);
        Assert.Contains("notifications", PluginManifestValidator.Validate(TestManifests.Valid with { Id = "notifications" }).Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void Without_an_MCP_server_UI_or_hub_those_fields_are_null()
    {
        var document = PluginManifestDocument.From(TestManifests.Valid with { Mcp = null, Ui = null });

        Assert.Null(document.Endpoints.Mcp);
        Assert.Null(document.Endpoints.Realtime);
        Assert.Null(document.Ui);
    }

    [Theory]
    [InlineData("hub", true)]
    [InlineData("live/chat", true)]
    [InlineData("/hub", false)]
    [InlineData("Hub", false)]
    [InlineData("api/hub", false)]
    [InlineData("ui", false)]
    [InlineData("", false)]
    public void The_realtime_hub_is_a_relative_path_of_its_own(string path, bool valid)
    {
        var problems = PluginManifestValidator.Validate(TestManifests.Valid with { Realtime = path });

        Assert.Equal(valid, problems.Count == 0);
        if (valid)
        {
            Assert.Equal($"/plugins/todo/{path}", PluginManifestDocument.From(TestManifests.Valid with { Realtime = path }).Endpoints.Realtime);
        }
    }

    [Fact]
    public void Json_has_the_shape_the_web_app_reads()
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(PluginManifestDocument.From(TestManifests.Valid), Json))!;

        Assert.Equal(1, json["manifestVersion"]!.GetValue<int>());
        Assert.Equal("2.1.0", json["requires"]!["homeApps"]!.GetValue<string>());
        Assert.Equal("Tasks", json["name"]!["en"]!.GetValue<string>());
        Assert.Equal("HouseholdUser", json["scopes"]![0]!.GetValue<string>());
        Assert.Equal("Admin", json["enablement"]!["minRole"]!.GetValue<string>());
        Assert.Equal("Member", json["permissions"]![1]!["minRole"]!.GetValue<string>());
        Assert.Equal("Wall", json["ui"]!["widgets"]![0]!["places"]![1]!.GetValue<string>());
        Assert.Equal("my_tasks", json["mcp"]!["tools"]![1]!.GetValue<string>());
    }
}

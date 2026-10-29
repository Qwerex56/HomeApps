using HomeApps.Contracts.Households;
using HomeApps.Plugins.Manifest;

namespace HomeApps.Plugins.Tests;

internal static class TestManifests
{
    public static PluginManifest Valid => new()
    {
        Id = "todo",
        Version = "1.0.0",
        MinHomeAppsVersion = "2.1.0",
        Name = new LocalizedText("Zadania", "Tasks"),
        Icon = "pi-check-square",
        Scopes = [DataScope.HouseholdUser],
        Enablement = new PluginEnablementRule(HouseholdRole.Admin),
        Permissions = [new PluginPermission("tasks.read", HouseholdRole.Guest), new PluginPermission("tasks.write", HouseholdRole.Member)],
        Ui = new PluginUi
        {
            Routes = [new PluginRoute("", new LocalizedText("Zadania", "Tasks"))],
            Widgets = [new PluginWidget("today", new LocalizedText("Na dziś", "Today"), WidgetSize.Medium, [WidgetPlace.Household, WidgetPlace.Wall])],
            Settings = true,
        },
        Display = [new DisplayScreen("today", new LocalizedText("Na dziś", "Today"), "households/{householdId}/display/today", 300)],
        Mcp = new PluginMcp(["add_task", "my_tasks"]),
        Events = new PluginEvents([], [typeof(MemberLeft)]),
    };
}

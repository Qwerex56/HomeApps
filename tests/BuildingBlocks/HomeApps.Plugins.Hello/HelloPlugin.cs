using HomeApps.Contracts.Households;
using HomeApps.Plugins.Manifest;

namespace HomeApps.Plugins.Hello;

public static class HelloPlugin
{
    public const string Read = "greetings.read";
    public const string Write = "greetings.write";

    public static PluginManifest Manifest { get; } = new()
    {
        Id = "hello",
        Version = "1.0.0",
        MinHomeAppsVersion = "2.1.0",
        Name = new LocalizedText("Powitania", "Greetings"),
        Description = new LocalizedText("Przykładowy plugin", "Sample plugin"),
        Icon = "pi-face-smile",
        Scopes = [DataScope.Household],
        Enablement = new PluginEnablementRule(HouseholdRole.Admin),
        Permissions =
        [
            new PluginPermission(Read, HouseholdRole.Guest),
            new PluginPermission(Write, HouseholdRole.Member),
        ],
        Ui = new PluginUi
        {
            Routes = [new PluginRoute("", new LocalizedText("Powitania", "Greetings"))],
            Widgets = [new PluginWidget("latest", new LocalizedText("Ostatnie powitanie", "Latest greeting"), WidgetSize.Small, [WidgetPlace.Household, WidgetPlace.Wall])],
        },
        Display = [new DisplayScreen("latest", new LocalizedText("Powitanie", "Greeting"), "households/{householdId}/greetings/latest", 300)],
        Events = new PluginEvents([], [typeof(MemberLeft)]),
    };
}

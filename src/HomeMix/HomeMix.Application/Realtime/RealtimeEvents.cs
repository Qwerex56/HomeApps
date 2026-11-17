using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Realtime;

/// <summary>The events of docs/homemix.md, "RealTime", and who gets each.</summary>
public static class RealtimeEvents
{
    /// <summary>A document was written or deleted: to whoever uses the installation, or for user data only to its owner.</summary>
    public static RealtimeMessage DataChanged(Guid installationId, InstallTarget target, DataScope scope, Guid? ownerUserId,
        string collection, string key, Guid by) =>
        new(
            scope == DataScope.User && ownerUserId is { } owner ? RealtimeGroups.User(owner) : RealtimeGroups.Of(target),
            "data.changed",
            new { installationId, scope = scope == DataScope.User ? "user" : "household", collection, key, by });

    /// <summary>Installed, updated, removed, blocked, or its widgets were chosen: the web app reads the list again.</summary>
    public static RealtimeMessage InstallationChanged(Installation installation, string change) =>
        new(
            RealtimeGroups.Of(installation.Target),
            "installation.changed",
            new { installationId = installation.Id, addonId = installation.AddonId, version = installation.Version, change });

    /// <summary>A household's widgets were arranged: its people read the board again (UI 2.4.1).</summary>
    public static RealtimeMessage BoardChanged(Guid householdId, string place) =>
        new(RealtimeGroups.Household(householdId), "board.changed", new { householdId, place });

    /// <summary>A push() from the add-on, shown as a toast to recipients who have the app open.</summary>
    public static RealtimeMessage Notification(Guid userId, Guid installationId, string title, string? body, string? path, string? tag) =>
        new(RealtimeGroups.User(userId), "notification", new { installationId, title, body, path, tag });

    /// <summary>The person's role in a household changed, or they left it: the web app asks for their permissions again.</summary>
    public static RealtimeMessage PermissionsChanged(Guid userId, Guid householdId, string? role) =>
        new(RealtimeGroups.User(userId), "permissions.changed", new { householdId, role });
}

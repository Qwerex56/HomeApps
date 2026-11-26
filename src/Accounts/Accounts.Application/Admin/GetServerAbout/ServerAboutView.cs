namespace Accounts.Application.Admin.GetServerAbout;

/// <param name="LatestVersion">The newest GitHub release, or <c>null</c> when it could not be read.</param>
/// <param name="IsUpdateAvailable"><c>null</c> when the newest release is unknown.</param>
public sealed record ServerAboutView(
    string CurrentVersion,
    string? LatestVersion,
    bool? IsUpdateAvailable,
    string? ReleaseUrl,
    IReadOnlyList<ServiceAboutView> Services,
    IReadOnlyList<PluginAboutView> Plugins);

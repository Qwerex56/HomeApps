using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Packages.AddPackageFromGitHub;

/// <summary>The <c>addon.zip</c> of a GitHub repository's latest release (or of <paramref name="Tag"/>), checked and kept.</summary>
public sealed record AddPackageFromGitHubCommand(string Repository, string? Tag) : ICommand<PackageView>;

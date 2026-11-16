using FluentResults;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;

namespace HomeMix.Application.Installations;

/// <summary>An installation whose widgets the caller may set: household admins and up, or the account's owner.</summary>
internal static class WidgetSettings
{
    public static async Task<Result<(Installation Installation, AddonPackage Package)>> FindAsync(
        Guid installationId,
        IInstallationRepository installations,
        IPackageRepository packages,
        InstallationAccess access,
        CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(installationId, cancellationToken);
        if (installation is null || !installation.IsActive || !await access.CanUseAsync(installation.Target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!await access.CanManageAsync(installation.Target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.Forbidden);
        }

        var package = await packages.FindAsync(installation.PackageSha256, cancellationToken);
        return package is null ? Result.Fail(InstallationErrors.NotFound) : (installation, package);
    }
}

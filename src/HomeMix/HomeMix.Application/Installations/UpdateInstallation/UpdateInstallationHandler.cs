using HomeMix.Application.Blocks;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Installations.UpdateInstallation;

internal sealed class UpdateInstallationHandler(
    IPackageRepository packages,
    IInstallationRepository installations,
    InstallationAccess access,
    ICurrentUser currentUser,
    TimeProvider time,
    Blocklist blocklist,
    IOptions<HomeMixOptions> options) : ICommandHandler<UpdateInstallationCommand, InstallationView>
{
    public async Task<Result<InstallationView>> Handle(UpdateInstallationCommand command, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(command.InstallationId, cancellationToken);
        if (installation is null || !installation.IsActive || !await access.CanUseAsync(installation.Target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!await access.CanManageAsync(installation.Target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.Forbidden);
        }

        if (!options.Value.UploadsEnabled)
        {
            return Result.Fail(PackageErrors.UploadsDisabled);
        }

        var package = await packages.FindAsync(command.Sha256.ToLowerInvariant(), cancellationToken);
        if (package is null)
        {
            return Result.Fail(PackageErrors.NotFound);
        }

        if (await blocklist.IsBlockedAsync(package.AddonId, package.Sha256, cancellationToken))
        {
            return Result.Fail(PackageErrors.Blocked);
        }

        var updated = installation.Update(package, command.Consent?.ToConsent(), currentUser.RequiredUserId, time.GetUtcNow());
        return updated.IsFailed ? updated : InstallationView.From(installation, package);
    }
}

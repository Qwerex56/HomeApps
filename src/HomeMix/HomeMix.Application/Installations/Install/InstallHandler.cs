using HomeMix.Application.Blocks;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Data;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;
using HomeMix.Application.Realtime;

namespace HomeMix.Application.Installations.Install;

internal sealed class InstallHandler(
    IPackageRepository packages,
    IInstallationRepository installations,
    IDocumentRepository documents,
    InstallationAccess access,
    ICurrentUser currentUser,
    TimeProvider time,
    Blocklist blocklist,
    IOptions<HomeMixOptions> options,
    RealtimeOutbox realtime) : ICommandHandler<InstallCommand, InstallationView>
{
    public async Task<Result<InstallationView>> Handle(InstallCommand command, CancellationToken cancellationToken)
    {
        var target = command.Target == TargetKind.Household
            ? InstallTarget.Household(command.HouseholdId!.Value)
            : InstallTarget.User(currentUser.RequiredUserId);
        if (!await access.CanManageAsync(target, cancellationToken))
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

        if (await installations.FindActiveAsync(package.AddonId, target, cancellationToken) is { } existing)
        {
            // The id is not unique in the store (homemix-2.md S3), but data and tools are by id, so one per place.
            var standing = await packages.FindAsync(existing.PackageSha256, cancellationToken);
            return standing is not null && !string.Equals(standing.Repository, package.Repository, StringComparison.OrdinalIgnoreCase)
                ? Result.Fail(InstallationErrors.IdInUse(package.AddonId, standing.Manifest.Name.Pl))
                : Result.Fail(InstallationErrors.AlreadyInstalled);
        }

        var installation = Installation.Install(package, target, command.Consent.ToConsent(), currentUser.RequiredUserId, time.GetUtcNow());
        if (installation.IsFailed)
        {
            return installation.ToResult();
        }

        installations.Add(installation.Value);
        realtime.Add(RealtimeEvents.InstallationChanged(installation.Value, "installed"));

        // Uninstalled within the retention time: its data comes back.
        var now = time.GetUtcNow();
        var earlier = await installations.RemovedSinceAsync(package.AddonId, target, now - DataRetention.AfterRemoval, cancellationToken);
        if (earlier.Count > 0)
        {
            foreach (var document in await documents.ListOfAsync(earlier, cancellationToken))
            {
                document.MoveTo(installation.Value.Id);
            }
        }

        return InstallationView.From(installation.Value, package);
    }
}

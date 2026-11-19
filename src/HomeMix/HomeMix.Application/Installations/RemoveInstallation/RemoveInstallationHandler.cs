using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using HomeMix.Application.Realtime;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Installations.RemoveInstallation;

internal sealed class RemoveInstallationHandler(
    IInstallationRepository installations,
    IPackageRepository packages,
    InstallationAccess access,
    ICurrentUser currentUser,
    TimeProvider time,
    RealtimeOutbox realtime,
    IStoreStatsRepository stats,
    ITriggerStates triggerStates)
    : ICommandHandler<RemoveInstallationCommand>
{
    public async Task<Result> Handle(RemoveInstallationCommand command, CancellationToken cancellationToken)
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

        realtime.Add(RealtimeEvents.InstallationChanged(installation, "removed"));
        if (await packages.FindAsync(installation.PackageSha256, cancellationToken) is { } package)
        {
            stats.Add(StoreEvent.Of(installation, package, StoreEventKind.Removed, time.GetUtcNow()));
        }

        await triggerStates.RemoveForInstallationAsync(installation.Id, cancellationToken);
        return installation.Remove(currentUser.RequiredUserId, time.GetUtcNow());
    }
}

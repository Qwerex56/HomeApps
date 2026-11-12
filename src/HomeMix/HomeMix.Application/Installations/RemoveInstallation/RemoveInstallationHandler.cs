using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.RemoveInstallation;

internal sealed class RemoveInstallationHandler(
    IInstallationRepository installations, InstallationAccess access, ICurrentUser currentUser, TimeProvider time)
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

        return installation.Remove(currentUser.RequiredUserId, time.GetUtcNow());
    }
}

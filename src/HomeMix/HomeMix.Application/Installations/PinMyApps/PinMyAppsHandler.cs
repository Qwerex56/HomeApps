using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.PinMyApps;

internal sealed class PinMyAppsHandler(IInstallationRepository installations, IPackageRepository packages, ICurrentUser currentUser)
    : ICommandHandler<PinMyAppsCommand, IReadOnlyList<InstallationView>>
{
    public async Task<Result<IReadOnlyList<InstallationView>>> Handle(PinMyAppsCommand command, CancellationToken cancellationToken)
    {
        var mine = await installations.ListActiveAsync(InstallTarget.User(currentUser.RequiredUserId), cancellationToken);
        if (command.InstallationIds.Any(id => mine.All(i => i.Id != id)))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        foreach (var installation in mine)
        {
            var position = command.InstallationIds.ToList().IndexOf(installation.Id);
            var pinned = installation.PinAt(position < 0 ? null : position);
            if (pinned.IsFailed)
            {
                return pinned;
            }
        }

        return Result.Ok(await InstallationViews.ForAsync(mine, packages, cancellationToken));
    }
}

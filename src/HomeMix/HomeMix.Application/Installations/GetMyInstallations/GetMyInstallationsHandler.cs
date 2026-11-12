using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.GetMyInstallations;

internal sealed class GetMyInstallationsHandler(IInstallationRepository installations, IPackageRepository packages, ICurrentUser currentUser)
    : IQueryHandler<GetMyInstallationsQuery, IReadOnlyList<InstallationView>>
{
    public async Task<Result<IReadOnlyList<InstallationView>>> Handle(GetMyInstallationsQuery query, CancellationToken cancellationToken)
    {
        var mine = await installations.ListActiveAsync(InstallTarget.User(currentUser.RequiredUserId), cancellationToken);
        return Result.Ok(await InstallationViews.ForAsync(mine, packages, cancellationToken));
    }
}

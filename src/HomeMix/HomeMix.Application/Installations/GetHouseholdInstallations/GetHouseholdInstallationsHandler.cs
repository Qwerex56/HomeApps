using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.GetHouseholdInstallations;

internal sealed class GetHouseholdInstallationsHandler(IInstallationRepository installations, IPackageRepository packages, InstallationAccess access)
    : IQueryHandler<GetHouseholdInstallationsQuery, IReadOnlyList<InstallationView>>
{
    public async Task<Result<IReadOnlyList<InstallationView>>> Handle(GetHouseholdInstallationsQuery query, CancellationToken cancellationToken)
    {
        var target = InstallTarget.Household(query.HouseholdId);
        if (!await access.CanUseAsync(target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        return Result.Ok(await InstallationViews.ForAsync(await installations.ListActiveAsync(target, cancellationToken), packages, cancellationToken));
    }
}

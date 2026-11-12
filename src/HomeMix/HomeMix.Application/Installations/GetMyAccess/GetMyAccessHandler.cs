using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.GetMyAccess;

internal sealed class GetMyAccessHandler(
    IInstallationRepository installations, IPackageRepository packages, IHouseholdRoles households, ICurrentUser currentUser)
    : IQueryHandler<GetMyAccessQuery, MyAccessView>
{
    public async Task<Result<MyAccessView>> Handle(GetMyAccessQuery query, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(query.InstallationId, cancellationToken);
        if (installation is null || !installation.IsActive)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        HouseholdRole? role = null;
        if (installation.TargetKind == TargetKind.Household)
        {
            role = await households.GetRoleAsync(installation.TargetId, cancellationToken);
            if (role is null)
            {
                return Result.Fail(InstallationErrors.NotFound);
            }
        }
        else if (installation.TargetId != currentUser.RequiredUserId)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        var package = await packages.FindAsync(installation.PackageSha256, cancellationToken);
        if (package is null)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        // On an account everything is the owner's.
        var granted = package.Manifest.Permissions
            .Where(p => role is not { } r || (Enum.TryParse<HouseholdRole>(p.MinRole, out var min) && r >= min))
            .Select(p => p.Id)
            .ToList();
        return new MyAccessView(role, granted);
    }
}

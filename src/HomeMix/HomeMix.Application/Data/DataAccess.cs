using HomeMix.Application.Blocks;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Data;

/// <summary>
/// Who reaches which data (docs/homemix.md, "Data"): whoever uses the installation reads; in a household, writing the
/// shared data needs the role of the add-on's permission <c>{collection}.write</c>, Member when it names none, and
/// everyone writes their own private data. The add-on must have been granted the scope's capability.
/// </summary>
public sealed class DataAccess(
    IInstallationRepository installations,
    IPackageRepository packages,
    IHouseholdRoles households,
    ICurrentUser currentUser,
    Blocklist blocklist)
{
    public const HouseholdRole DefaultWriteRole = HouseholdRole.Member;

    public async Task<Result<DataSpace>> OpenAsync(Guid installationId, DataScope scope, string collection, bool write, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(installationId, cancellationToken);
        if (installation is null || !installation.IsActive || await blocklist.IsBlockedAsync(installation.AddonId, installation.PackageSha256, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        var me = currentUser.RequiredUserId;
        HouseholdRole? role = null;
        if (installation.TargetKind == TargetKind.Household)
        {
            role = await households.GetRoleAsync(installation.TargetId, cancellationToken);
            if (role is null)
            {
                return Result.Fail(InstallationErrors.NotFound);
            }
        }
        else if (installation.TargetId != me)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        var capability = scope == DataScope.User ? AddonCapabilities.StorageUser : AddonCapabilities.StorageHousehold;
        if (!installation.Capabilities.Contains(capability))
        {
            return Result.Fail(DataErrors.CapabilityMissing(capability));
        }

        if (write && scope == DataScope.Household && role is { } myRole)
        {
            var package = await packages.FindAsync(installation.PackageSha256, cancellationToken);
            if (package is null || myRole < WriteRole(package.Manifest, collection))
            {
                return Result.Fail(DataErrors.ReadOnly);
            }
        }

        return new DataSpace(installation.Id, scope, scope == DataScope.User ? me : null);
    }

    public static HouseholdRole WriteRole(AddonManifest manifest, string collection)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var permission = manifest.Permissions.FirstOrDefault(p => p.Id == $"{collection}.write");
        return permission is not null && Enum.TryParse<HouseholdRole>(permission.MinRole, out var role) ? role : DefaultWriteRole;
    }
}

using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Data;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;

namespace HomeMix.Application.Devices;

/// <summary>
/// What an add-on may do with devices (household-jarvis-devices.md D8): only an installation in a household, only for
/// someone who uses it there, only with the capability the add-on was granted at install (<c>devices</c> to read,
/// <c>devices.control</c> to set), and only with the devices of that household. Devices then applies the person's own role.
/// </summary>
internal sealed class DevicesAccess(
    IInstallationRepository installations,
    IPackageRepository packages,
    IHouseholdRoles households,
    IHouseholdDevices devices,
    ICurrentUser currentUser,
    Blocklist blocklist)
{
    public async Task<Result<Installation>> OpenAsync(Guid installationId, string capability, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(installationId, cancellationToken);
        if (installation is null || !installation.IsActive || await blocklist.IsBlockedAsync(installation.AddonId, installation.PackageSha256, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (installation.TargetKind != TargetKind.Household)
        {
            return installation.TargetId == currentUser.RequiredUserId ? Result.Fail(DeviceErrors.HouseholdOnly) : Result.Fail(InstallationErrors.NotFound);
        }

        if (await households.GetRoleAsync(installation.TargetId, cancellationToken) is null)
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        return installation.Capabilities.Contains(capability) ? installation : Result.Fail(DataErrors.CapabilityMissing(capability));
    }

    public async Task<Result<AddonPackage>> PackageOfAsync(Installation installation, CancellationToken cancellationToken)
    {
        var package = await packages.FindAsync(installation.PackageSha256, cancellationToken);
        return package is null ? Result.Fail(InstallationErrors.NotFound) : package;
    }

    /// <summary>The device must be in the installation's household; anything else is "not found", like a device that does not exist.</summary>
    public async Task<Result> EnsureInHouseholdAsync(Installation installation, Guid deviceId, CancellationToken cancellationToken)
    {
        var inHousehold = await devices.ListAsync(installation.TargetId, null, null, null, false, cancellationToken);
        return inHousehold.Any(d => d.Id == deviceId) ? Result.Ok() : Result.Fail(DeviceErrors.NotFound);
    }
}

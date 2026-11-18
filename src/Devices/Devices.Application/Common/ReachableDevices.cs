using Devices.Application.Abstractions;
using Devices.Domain.Devices;

namespace Devices.Application.Common;

/// <summary>
/// The devices JARVIS may use for the caller (S3): their own, then those of every household they are in. A slug is
/// taken by the first device that has it, so the caller's own device wins over a household's of the same name.
/// </summary>
internal static class ReachableDevices
{
    public static async Task<IReadOnlyList<Device>> OfCallerAsync(
        IDeviceRepository devices, IHouseholdRoles households, Guid userId, CancellationToken cancellationToken)
    {
        var own = await devices.OfOwnerAsync(userId, cancellationToken);
        var householdIds = await households.GetMyHouseholdIdsAsync(cancellationToken);
        var shared = householdIds.Count == 0 ? [] : await devices.OfHouseholdsAsync(householdIds, cancellationToken);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        return [.. own.OrderBy(d => d.Name, StringComparer.Ordinal)
            .Concat(shared.OrderBy(d => d.Name, StringComparer.Ordinal))
            .Where(d => !d.IsDisabled && seen.Add(d.Slug))];
    }
}

using Devices.Application.Abstractions;
using Devices.Domain.Devices;

namespace Devices.Application.Common;

/// <summary>
/// The devices JARVIS may use for the caller (S3): their own, then those of every household they are in. A slug is unique only
/// per scope (one person's devices, one household), so the same slug can come from two scopes. The first device in a fixed order
/// (own first, then by creation) keeps it; every later one gets a short part of its id after it (<c>fotel-3f2a</c>), so each
/// tool name points at exactly one device and the choice never depends on a lookup that takes "the first".
/// </summary>
internal static class ReachableDevices
{
    public static async Task<IReadOnlyList<ReachableDevice>> OfCallerAsync(
        IDeviceRepository devices, IHouseholdRoles households, Guid userId, CancellationToken cancellationToken)
    {
        var own = await devices.OfOwnerAsync(userId, cancellationToken);
        var householdIds = await households.GetMyHouseholdIdsAsync(cancellationToken);
        var shared = householdIds.Count == 0 ? [] : await devices.OfHouseholdsAsync(householdIds, cancellationToken);
        return Name(own.Concat(shared).Where(d => !d.IsDisabled));
    }

    /// <summary>Gives each device its tool slug, in the order they come (callers order them: own devices first).</summary>
    public static IReadOnlyList<ReachableDevice> Name(IEnumerable<Device> inOrder)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var named = new List<ReachableDevice>();
        foreach (var device in inOrder.OrderBy(d => d.IsHouseholdDevice).ThenBy(d => d.CreatedAt).ThenBy(d => d.Id))
        {
            var slug = device.Slug;
            for (var length = 4; !taken.Add(slug); length += 2)
            {
                var suffix = "-" + device.Id.ToString("N")[..Math.Min(length, 32)];
                slug = device.Slug[..Math.Min(device.Slug.Length, DeviceSlug.MaxLength - suffix.Length)].TrimEnd('-') + suffix;
            }

            named.Add(new ReachableDevice(device, slug));
        }

        return named;
    }
}

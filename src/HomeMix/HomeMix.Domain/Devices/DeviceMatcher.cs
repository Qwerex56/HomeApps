using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Devices;

/// <summary>
/// Finds a widget's devices (household-jarvis-devices.md 2.5): capabilities first, then roles, then model, and the
/// manual pick last. An entry ranks by its best field (capability 0, role 1, model 2); an entry with
/// <c>pick: manual</c> ranks after all others, so its devices are only a list to choose from. Among equal ranks the
/// online ones come first, then by name. The saved pick wins when it is still a candidate; otherwise the first
/// device that matched without a manual entry is shown, and when only manual entries matched the widget needs a pick.
/// </summary>
public static class DeviceMatcher
{
    private const int ManualRank = 3;

    public static DeviceMatches Match(IReadOnlyList<DeviceMatch> rules, IEnumerable<DeviceCandidate> devices, Guid? picked)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(devices);

        var ranked = devices
            .Select(d => (Device: d, Rank: BestRank(rules, d)))
            .Where(x => x.Rank is not null)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Device.Online ? 0 : 1)
            .ThenBy(x => x.Device.Name, StringComparer.Ordinal)
            .ToList();
        var candidates = ranked.Select(x => x.Device).ToList();

        if (picked is { } id && candidates.Exists(d => d.Id == id))
        {
            return new DeviceMatches(candidates, id, false);
        }

        var automatic = ranked.FirstOrDefault(x => x.Rank < ManualRank);
        return automatic.Device is null
            ? new DeviceMatches(candidates, null, candidates.Count > 0)
            : new DeviceMatches(candidates, automatic.Device.Id, false);
    }

    /// <summary>Does the device satisfy every field the rule gives? A capability matches with or without an instance id.</summary>
    public static bool Fits(DeviceMatch rule, DeviceCandidate device)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(device);
        return (rule.Capability is null || device.Capabilities.Any(c => c == rule.Capability || c.StartsWith(rule.Capability + ":", StringComparison.Ordinal)))
            && (rule.Role is null || device.Roles.Contains(rule.Role, StringComparer.Ordinal))
            && (rule.Model is null || string.Equals(device.Model, rule.Model, StringComparison.Ordinal));
    }

    private static int? BestRank(IReadOnlyList<DeviceMatch> rules, DeviceCandidate device)
    {
        int? best = null;
        foreach (var rule in rules.Where(r => Fits(r, device)))
        {
            var rank = (rule.Capability is not null ? 0 : rule.Role is not null ? 1 : 2) + (rule.IsManual ? ManualRank : 0);
            best = best is null || rank < best ? rank : best;
        }

        return best;
    }
}

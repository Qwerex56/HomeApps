using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// Memberships per (household, user) for a short time, in this process only. <see cref="HouseholdAccessConsumer"/> drops
/// entries the moment Households reports a change, so removing someone works at once.
/// </summary>
public sealed class HouseholdAccessCache(TimeProvider time, IOptions<PluginHostingOptions> options)
{
    private readonly ConcurrentDictionary<(Guid HouseholdId, Guid UserId), Entry> _entries = new();

    public bool TryGet(Guid householdId, Guid userId, out HouseholdMembership? membership)
    {
        membership = null;
        if (!_entries.TryGetValue((householdId, userId), out var entry))
        {
            return false;
        }

        if (entry.ExpiresAt <= time.GetUtcNow())
        {
            _entries.TryRemove(new KeyValuePair<(Guid, Guid), Entry>((householdId, userId), entry));
            return false;
        }

        membership = entry.Membership;
        return true;
    }

    public void Set(Guid householdId, Guid userId, HouseholdMembership? membership)
    {
        var seconds = membership is null ? options.Value.NoAccessCacheSeconds : options.Value.AccessCacheSeconds;
        if (seconds == 0)
        {
            return;
        }

        _entries[(householdId, userId)] = new Entry(membership, time.GetUtcNow().AddSeconds(seconds));
    }

    public void Forget(Guid householdId, Guid userId) => _entries.TryRemove((householdId, userId), out _);

    public void Forget(Guid householdId)
    {
        foreach (var key in _entries.Keys.Where(k => k.HouseholdId == householdId))
        {
            _entries.TryRemove(key, out _);
        }
    }

    private sealed record Entry(HouseholdMembership? Membership, DateTimeOffset ExpiresAt);
}

using MacDeviceEmulator.Protocol;

namespace MacDeviceEmulator.Services;

/// <summary>Tracks actions awaiting screen-only user confirmation.</summary>
public sealed class PendingConfirmationStore
{
    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, VoiceActionView> _pending = [];

    public IReadOnlyList<VoiceActionView> AddFromReply(IEnumerable<VoiceActionView>? actions, DateTimeOffset now)
    {
        if (actions is null)
        {
            return [];
        }

        var added = new List<VoiceActionView>();
        lock (_gate)
        {
            PruneExpiredLocked(now);
            foreach (var action in actions)
            {
                if (!IsNeedsConfirmation(action))
                {
                    _pending.Remove(action.Id);
                    continue;
                }

                if (action.ConfirmBy is { } confirmBy && confirmBy <= now)
                {
                    continue;
                }

                _pending[action.Id] = action;
                added.Add(action);
            }
        }

        return added;
    }

    public void Remove(Guid id)
    {
        lock (_gate)
        {
            _pending.Remove(id);
        }
    }

    public bool HasPending(DateTimeOffset now)
    {
        lock (_gate)
        {
            PruneExpiredLocked(now);
            return _pending.Count > 0;
        }
    }

    public IReadOnlyList<VoiceActionView> GetActive(DateTimeOffset now)
    {
        lock (_gate)
        {
            PruneExpiredLocked(now);
            return _pending.Values.ToList();
        }
    }

    public VoiceActionView? ResolveTarget(string? token, DateTimeOffset now)
    {
        lock (_gate)
        {
            PruneExpiredLocked(now);
            if (_pending.Count == 0)
            {
                return null;
            }

            var list = _pending.Values.ToList();
            if (string.IsNullOrWhiteSpace(token))
            {
                return list.Count == 1 ? list[0] : null;
            }

            var trimmed = token.Trim();
            if (Guid.TryParse(trimmed, out var id) && _pending.TryGetValue(id, out var exact))
            {
                return exact;
            }

            if (int.TryParse(trimmed, out var oneBasedIndex) && oneBasedIndex >= 1 && oneBasedIndex <= list.Count)
            {
                return list[oneBasedIndex - 1];
            }

            return list.FirstOrDefault(a => a.Id.ToString("N").StartsWith(trimmed, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void PruneExpiredLocked(DateTimeOffset now)
    {
        var expiredIds = _pending
            .Where(kv => kv.Value.ConfirmBy is { } confirmBy && confirmBy <= now)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var id in expiredIds)
        {
            _pending.Remove(id);
        }
    }

    private static bool IsNeedsConfirmation(VoiceActionView action) =>
        string.Equals(action.Status, "NeedsConfirmation", StringComparison.OrdinalIgnoreCase)
        || string.Equals(action.Status, "2", StringComparison.Ordinal);
}

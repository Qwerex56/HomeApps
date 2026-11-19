using System.Collections.Concurrent;
using Devices.Application.Abstractions;

namespace Devices.Application.Common;

/// <summary>In memory, like the device links: there is one Devices instance, and losing it only drops a 5 second attribution.</summary>
internal sealed class RecentControlSources(TimeProvider time) : IControlSources
{
    private readonly ConcurrentDictionary<(Guid DeviceId, string Control), (string Source, DateTimeOffset Until)> _sets = new();

    public void Remember(Guid deviceId, string control, string source)
    {
        var now = time.GetUtcNow();
        foreach (var (key, value) in _sets)
        {
            if (value.Until <= now)
            {
                _sets.TryRemove(key, out _);
            }
        }

        _sets[(deviceId, control)] = (source, now + IControlSources.Window);
    }

    public string? Find(Guid deviceId, IEnumerable<string> controls)
    {
        var now = time.GetUtcNow();
        foreach (var control in controls)
        {
            if (_sets.TryGetValue((deviceId, control), out var set) && set.Until > now)
            {
                return set.Source;
            }
        }

        return null;
    }
}

using System.Collections.Concurrent;

namespace HomeMix.Application.Triggers;

/// <summary>At most <see cref="PerMinute"/> trigger runs per installation in any minute (household-jarvis-devices.md 2.6); in memory, like the rest of a single instance's counters.</summary>
public sealed class TriggerRunLimiter(TimeProvider time)
{
    public const int PerMinute = 30;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<Guid, Queue<DateTimeOffset>> _runs = new();

    /// <summary>Counts a run and says yes, or says no (and counts nothing) when the installation already ran <see cref="PerMinute"/> times in the last minute.</summary>
    public bool TryAcquire(Guid installationId)
    {
        var now = time.GetUtcNow();
        var runs = _runs.GetOrAdd(installationId, _ => new Queue<DateTimeOffset>());
        lock (runs)
        {
            while (runs.Count > 0 && now - runs.Peek() >= Window)
            {
                runs.Dequeue();
            }

            if (runs.Count >= PerMinute)
            {
                return false;
            }

            runs.Enqueue(now);
            return true;
        }
    }
}

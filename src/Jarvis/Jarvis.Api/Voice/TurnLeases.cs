using Microsoft.Extensions.Caching.Memory;

namespace Jarvis.Api.Voice;

/// <summary>
/// One turn at a time per person (jarvis-desktop-rework 2.2 E): the first computer to speak holds
/// <c>turn_lease:{userId}</c> for five seconds or until its turn ends; a second one is told <c>lease_conflict</c> and keeps quiet.
/// </summary>
internal sealed class TurnLeases(IMemoryCache cache)
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);

    private readonly object _gate = new();

    /// <summary>True when the session holds the lease now (it may renew its own); false when another session does.</summary>
    public bool TryAcquire(Guid userId, Guid session)
    {
        lock (_gate)
        {
            if (cache.TryGetValue(Key(userId), out Guid holder) && holder != session)
            {
                return false;
            }

            cache.Set(Key(userId), session, Duration);
            return true;
        }
    }

    /// <summary>Ends the lease early, only if the session still holds it.</summary>
    public void Release(Guid userId, Guid session)
    {
        lock (_gate)
        {
            if (cache.TryGetValue(Key(userId), out Guid holder) && holder == session)
            {
                cache.Remove(Key(userId));
            }
        }
    }

    private static string Key(Guid userId) => $"turn_lease:{userId}";
}

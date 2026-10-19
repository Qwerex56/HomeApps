namespace Auth.Domain.Identities;

/// <summary>R5: five failed passwords in a row lock the identity for 15 minutes; a successful sign-in resets the count.</summary>
public sealed record Lockout
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(15);

    public static readonly Lockout None = new(0, null);

    private Lockout(int failedAttempts, DateTimeOffset? lockedUntil)
    {
        FailedAttempts = failedAttempts;
        LockedUntil = lockedUntil;
    }

    public int FailedAttempts { get; }

    public DateTimeOffset? LockedUntil { get; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil > now;

    /// <summary>Counts one more failure; the fifth in a row (after any earlier lock ran out) locks.</summary>
    public Lockout RegisterFailure(DateTimeOffset now)
    {
        var attempts = LockedUntil is not null && LockedUntil <= now ? 1 : FailedAttempts + 1;
        return attempts >= MaxFailedAttempts
            ? new Lockout(attempts, now + Duration)
            : new Lockout(attempts, null);
    }
}

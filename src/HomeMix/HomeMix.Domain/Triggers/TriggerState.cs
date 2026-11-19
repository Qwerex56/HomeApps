namespace HomeMix.Domain.Triggers;

/// <summary>
/// How one trigger of one installation is doing (household-jarvis-devices.md 2.6): whether it may run, how many runs in a row
/// failed, and the last run. A trigger without a row is enabled. After <see cref="MaxFailures"/> failures in a row it turns
/// itself off; an admin turns it back on.
/// </summary>
public sealed class TriggerState
{
    public const int MaxFailures = 5;
    public const int MaxErrorLength = 500;

    private TriggerState() { }

    public Guid InstallationId { get; private set; }

    public string TriggerId { get; private set; } = string.Empty;

    public bool Enabled { get; private set; } = true;

    public int ConsecutiveFailures { get; private set; }

    public DateTimeOffset? LastRunAt { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>The trigger turned itself off after too many failures (and an admin was told); false when an admin turned it off.</summary>
    public bool DisabledByFailures { get; private set; }

    public static TriggerState For(Guid installationId, string triggerId) => new() { InstallationId = installationId, TriggerId = triggerId };

    public void Succeeded(DateTimeOffset now)
    {
        LastRunAt = now;
        LastError = null;
        ConsecutiveFailures = 0;
    }

    /// <summary>Returns true when this failure turned the trigger off.</summary>
    public bool Failed(string error, DateTimeOffset now)
    {
        LastRunAt = now;
        LastError = error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
        ConsecutiveFailures++;
        if (!Enabled || ConsecutiveFailures < MaxFailures)
        {
            return false;
        }

        Enabled = false;
        DisabledByFailures = true;
        return true;
    }

    public void Enable()
    {
        Enabled = true;
        DisabledByFailures = false;
        ConsecutiveFailures = 0;
    }

    public void Disable()
    {
        Enabled = false;
        DisabledByFailures = false;
    }
}

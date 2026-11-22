namespace MacDeviceEmulator.Abstractions;

/// <summary>Detects whether the desktop user session is currently locked.</summary>
public interface ISessionLockMonitor
{
    Task<bool> IsLockedAsync(CancellationToken cancellationToken = default);
}

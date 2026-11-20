namespace MacDeviceEmulator.Abstractions;

/// <summary>Abstractions for system commands executed by the emulator on macOS.</summary>
public interface IMacSystem
{
    Task<int> GetVolumeAsync(CancellationToken cancellationToken = default);

    Task SetVolumeAsync(int volume, CancellationToken cancellationToken = default);

    Task<bool> IsMutedAsync(CancellationToken cancellationToken = default);

    Task SetMutedAsync(bool muted, CancellationToken cancellationToken = default);

    Task<double> GetCpuLoadAsync(CancellationToken cancellationToken = default);

    Task<int?> GetBatteryLevelAsync(CancellationToken cancellationToken = default);

    Task SayAsync(string text, CancellationToken cancellationToken = default);
}

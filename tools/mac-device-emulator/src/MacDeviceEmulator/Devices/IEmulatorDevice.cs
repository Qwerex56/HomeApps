namespace MacDeviceEmulator.Devices;

public interface IEmulatorDevice
{
    string DeviceKey { get; }
    string Name { get; }
    Task RunAsync(CancellationToken cancellationToken);
}

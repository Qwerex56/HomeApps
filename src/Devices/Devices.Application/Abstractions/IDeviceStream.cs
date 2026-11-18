namespace Devices.Application.Abstractions;

/// <summary>Pushes what happens to a household's devices to the open streams (SSE) of its members; in memory, one Devices instance.</summary>
public interface IDeviceStream
{
    void Publish(DeviceStreamMessage message);
}

using Devices.Application.Abstractions;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeStream : IDeviceStream
{
    public List<DeviceStreamMessage> Messages { get; } = [];

    public void Publish(DeviceStreamMessage message) => Messages.Add(message);
}

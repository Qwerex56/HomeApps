using System.Threading.Channels;
using Devices.Application.Abstractions;

namespace Devices.Api.Stream;

/// <summary>One open stream of a household: a small queue that drops its oldest message when the reader is slow.</summary>
internal sealed class StreamSubscription(DeviceStream owner, Guid householdId, bool members) : IDisposable
{
    private readonly Channel<DeviceStreamMessage> _queue = Channel.CreateBounded<DeviceStreamMessage>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public Guid HouseholdId => householdId;

    /// <summary>A member also gets events; a guest gets state only (D9).</summary>
    public bool Members => members;

    public ChannelReader<DeviceStreamMessage> Reader => _queue.Reader;

    public void Write(DeviceStreamMessage message) => _queue.Writer.TryWrite(message);

    public void Dispose() => owner.Remove(this);
}

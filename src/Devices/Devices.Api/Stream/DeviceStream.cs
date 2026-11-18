using System.Collections.Concurrent;
using Devices.Application.Abstractions;

namespace Devices.Api.Stream;

/// <summary>The open streams (SSE) of this instance; a message goes to those of its household, events only to members.</summary>
internal sealed class DeviceStream : IDeviceStream
{
    private readonly ConcurrentDictionary<StreamSubscription, byte> _subscriptions = new();

    public StreamSubscription Subscribe(Guid householdId, bool members)
    {
        var subscription = new StreamSubscription(this, householdId, members);
        _subscriptions[subscription] = 0;
        return subscription;
    }

    public void Publish(DeviceStreamMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        foreach (var subscription in _subscriptions.Keys)
        {
            if (subscription.HouseholdId == message.HouseholdId && (!message.MembersOnly || subscription.Members))
            {
                subscription.Write(message);
            }
        }
    }

    internal void Remove(StreamSubscription subscription) => _subscriptions.TryRemove(subscription, out _);
}

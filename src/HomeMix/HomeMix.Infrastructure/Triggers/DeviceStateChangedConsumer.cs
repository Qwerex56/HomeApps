using System.Text.Json;
using HomeApps.Contracts.Devices;
using HomeMix.Application.Triggers;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;
using MassTransit;

namespace HomeMix.Infrastructure.Triggers;

/// <summary>A household device reported new values: the household's add-ons' state triggers that match it run (docs/homemix.md, "Triggers").</summary>
public sealed class DeviceStateChangedConsumer(TriggerDispatcher dispatcher) : IConsumer<DeviceStateChanged>
{
    public async Task Consume(ConsumeContext<DeviceStateChanged> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var m = context.Message;
        if (m.Device is not { } device)
        {
            return;
        }

        using var changed = JsonDocument.Parse(m.Changed);
        await dispatcher.DispatchAsync(
            new TriggerOccurrence(
                m.HouseholdId, new DeviceCandidate(m.DeviceId, device.Name, true, device.Model, device.Roles, device.Capabilities), m.Slug,
                TriggerKind.State, changed.RootElement.Clone(), null, null, m.ChangedBy, m.At),
            context.CancellationToken);
    }
}

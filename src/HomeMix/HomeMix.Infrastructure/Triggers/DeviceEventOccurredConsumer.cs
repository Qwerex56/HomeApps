using System.Text.Json;
using HomeApps.Contracts.Devices;
using HomeMix.Application.Triggers;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;
using MassTransit;

namespace HomeMix.Infrastructure.Triggers;

/// <summary>A household device published an event: the household's add-ons' event triggers that match it run (docs/homemix.md, "Triggers").</summary>
public sealed class DeviceEventOccurredConsumer(TriggerDispatcher dispatcher) : IConsumer<DeviceEventOccurred>
{
    public async Task Consume(ConsumeContext<DeviceEventOccurred> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var m = context.Message;
        if (m.Device is not { } device)
        {
            return;
        }

        using var payload = m.Payload is null ? null : JsonDocument.Parse(m.Payload);
        await dispatcher.DispatchAsync(
            new TriggerOccurrence(
                m.HouseholdId, new DeviceCandidate(m.DeviceId, device.Name, true, device.Model, device.Roles, device.Capabilities), m.Slug,
                TriggerKind.Event, null, m.Name, payload?.RootElement.Clone(), null, m.OccurredAt),
            context.CancellationToken);
    }
}

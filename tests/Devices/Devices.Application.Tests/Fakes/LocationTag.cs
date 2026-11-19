using Devices.Domain.Devices;

namespace Devices.Application.Tests.Fakes;

/// <summary>A household presence tag: capability <c>geo</c>, so its values are for Member and up (H-4c item 9).</summary>
internal static class LocationTag
{
    public const string Descriptor = """
        { "format": "homeapps.device/1", "model": "a/znacznik", "version": "1.0.0", "purpose": "smart_home",
          "capabilities": [ { "type": "geo" } ],
          "sensors": [ { "name": "location", "value": { "type": "object" } }, { "name": "battery", "value": { "type": "number" } } ],
          "events": [ { "name": "seen", "data": { "type": "object" } } ] }
        """;

    public static Device Create(DateTimeOffset now, Guid household) => PairedDevice.Create(now, Descriptor, household);

    /// <summary>State with the values <c>{"location":{"room":"kitchen"}}</c> and health (battery 41) so tests see what is hidden and what stays.</summary>
    public static DeviceState StateOf(Device device, DateTimeOffset now)
    {
        var state = DeviceState.For(device.Id);
        state.ApplyValues(System.Text.Json.JsonDocument.Parse("""{ "location": { "room": "kitchen" } }""").RootElement, now);
        state.ApplyStatus(new StatusReport(41, null, -60, null, null, null), now);
        return state;
    }
}

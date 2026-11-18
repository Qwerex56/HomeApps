using Devices.Domain.Capabilities;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;

namespace Devices.Application.Tests.Fakes;

internal static class PairedDevice
{
    public const string Chair = """
        { "format": "homeapps.device/1", "model": "a/fotel", "version": "1.0.0", "purpose": "smart_home", "heartbeat": 60,
          "controls": [ { "name": "heating", "value": { "type": "integer", "minimum": 0, "maximum": 3 } } ],
          "sensors": [ { "name": "temperature", "value": { "type": "number" } } ],
          "events": [ { "name": "button", "data": { "type": "object", "properties": { "count": { "type": "integer" } } } },
                      { "name": "tag", "location": true } ] }
        """;

    /// <summary>A household device that was added, paired and sent <paramref name="descriptor"/>.</summary>
    public static Device Create(DateTimeOffset now, string? descriptor = Chair, Guid? householdId = null)
    {
        var device = Device.AddToHousehold(householdId ?? Guid.NewGuid(), Guid.NewGuid(), "Fotel", "hash", [], now).Value;
        device.Pair(now);
        if (descriptor is not null)
        {
            var parsed = DescriptorParser.Parse(descriptor).Value;
            device.ApplyDescriptor(parsed, CapabilityResolver.Resolve(parsed, CapabilityCatalog.BuiltIn, new Dictionary<string, RegisteredCapability>(), true));
        }

        return device;
    }
}

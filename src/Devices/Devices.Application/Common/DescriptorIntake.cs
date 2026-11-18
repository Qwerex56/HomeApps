using Devices.Application.Abstractions;
using Devices.Domain.Capabilities;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentResults;

namespace Devices.Application.Common;

/// <summary>
/// A device sent its descriptor, at pairing or on a connection: parse and check it, decide its capabilities (built-in,
/// namespaced with a definition, unknown: ignored with a warning), register the new namespaced ones instance-wide and
/// store it all on the device, replacing what it sent before.
/// </summary>
internal sealed class DescriptorIntake(ICapabilityRegistry registry, TimeProvider time)
{
    public async Task<Result<DeviceDescriptor>> ApplyAsync(Device device, string json, CancellationToken cancellationToken)
    {
        var parsed = DescriptorParser.Parse(json);
        if (parsed.IsFailed)
        {
            return parsed;
        }

        var resolution = CapabilityResolver.Resolve(parsed.Value, CapabilityCatalog.BuiltIn);
        var applied = device.ApplyDescriptor(parsed.Value, resolution);
        if (applied.IsFailed)
        {
            return applied;
        }

        var now = time.GetUtcNow();
        foreach (var definition in resolution.ToRegister)
        {
            if (await registry.FindAsync(definition.Name, cancellationToken) is { } known)
            {
                known.Extend(definition, now);
            }
            else
            {
                registry.Add(RegisteredCapability.Register(definition, now));
            }
        }

        return parsed;
    }
}

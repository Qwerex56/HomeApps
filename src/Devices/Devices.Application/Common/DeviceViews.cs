using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;

namespace Devices.Application.Common;

internal static class DeviceViews
{
    public static DeviceView ToView(this Device device, IDeviceLinks links, bool includeNotes = false, DeviceStateView? state = null)
    {
        var parsed = device.Descriptor is not null && DescriptorParser.Parse(device.Descriptor) is { IsSuccess: true } p ? p.Value : null;
        return new(
            device.Id,
            device.Name,
            device.Slug,
            device.Kind.ToString(),
            device.Purpose.ToWire(),
            device.HouseholdId,
            !device.IsHouseholdDevice || device.PairedAt is not null,
            !device.IsDisabled && links.IsOnline(device.Id),
            device.IsDisabled,
            device.CreatedAt,
            device.LastSeenAt,
            [.. device.Actions.Select(a => new DeviceActionView(a.Name, a.Description, a.ReadOnly, a.Confirm, a.AutoApprove))],
            new DeviceDescriptorView(
                device.Descriptor is not null,
                device.Model,
                device.DescriptorVersion,
                device.Roles,
                device.Capabilities,
                device.Warnings,
                [.. (parsed?.Controls ?? []).Select(c => new DescriptorControlView(c.Name, c.Label, c.Description, Schema(c.Value), c.Widget))],
                [.. (parsed?.Sensors ?? []).Select(s => new DescriptorSensorView(s.Name, s.Label, Schema(s.Value), s.Unit))],
                [.. (parsed?.Events ?? []).Select(e => new DescriptorEventView(e.Name, e.Label, e.Notify, parsed!.IsLocation(e)))],
                includeNotes ? parsed?.Jarvis : null,
                parsed?.Sleepy ?? false),
            includeNotes ? device.AdminNotes : null,
            device.JarvisMayControl,
            device.JarvisAsks,
            state);
    }

    private static JsonElement Schema(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}


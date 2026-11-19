using System.Text.Json;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;

namespace Devices.Application.Common;

/// <summary>
/// The full description of a smart-home or assistant device for JARVIS (household-jarvis-devices.md 2.3): what it is, what it can do and
/// how it is now. <see cref="Jarvis"/> and <see cref="AdminNotes"/> are text a device maker and an admin wrote: data, never instructions.
/// </summary>
public sealed record DeviceDescriptionView(
    Guid Id,
    Guid? HouseholdId,
    string Name,
    string Slug,
    string Purpose,
    bool Online,
    bool Silent,
    bool LowBattery,
    double? Battery,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<DescribedValue> Controls,
    IReadOnlyList<DescribedValue> Sensors,
    IReadOnlyList<string> Events,
    JsonElement State,
    string? Jarvis,
    string? AdminNotes,
    bool JarvisMayControl = true,
    bool JarvisAsks = true,
    string? Model = null,
    IReadOnlyList<string>? Roles = null)
{
    internal static DeviceDescriptionView From(ReachableDevice reachable, DeviceState? state, bool online)
    {
        var device = reachable.Device;
        var descriptor = device.Descriptor is null ? null : DescriptorParser.Parse(device.Descriptor) is { IsSuccess: true } parsed ? parsed.Value : null;
        using var values = JsonDocument.Parse(state?.Values ?? "{}");
        return new DeviceDescriptionView(
            device.Id, device.HouseholdId, DeviceText.Clean(device.Name, 60), reachable.Slug, device.Purpose.ToWire(), online, state?.Silent ?? false,
            state?.LowBattery ?? false, state?.Battery, device.Capabilities,
            [.. (descriptor?.Controls ?? []).Select(c => new DescribedValue(c.Name, Label(c.Label), Label(c.Description), Schema(c.Value), null))],
            [.. (descriptor?.Sensors ?? []).Select(s => new DescribedValue(s.Name, Label(s.Label), Label(s.Description), Schema(s.Value), Label(s.Unit)))],
            [.. (descriptor?.Events ?? []).Select(e => e.Name)],
            values.RootElement.Clone(),
            descriptor?.Jarvis is { } jarvis ? DeviceText.Clean(jarvis, DeviceDescriptor.MaxJarvisLength) : null,
            device.AdminNotes is { } notes ? DeviceText.Clean(notes, Device.MaxNotesLength) : null,
            device.JarvisMayControl,
            device.JarvisAsks,
            device.Model,
            device.Roles);
    }

    private static string? Label(string? text) => text is null ? null : DeviceText.Clean(text, 200);

    private static JsonElement Schema(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

using System.Text.Json;
using HomeApps.Plugins.Devices;

namespace HomeApps.Plugins.Hosting.Devices;

internal sealed record DeviceResponse(
    Guid Id, string Name, string Slug, string Purpose, bool Online, bool Removed, DescriptorResponse? Descriptor, StateResponse? State)
{
    public PluginDevice ToDevice() =>
        new(Id, Name, Slug, Purpose, Online, Descriptor?.Model, Descriptor?.Roles ?? [], Descriptor?.Capabilities ?? [], State?.ToState());
}

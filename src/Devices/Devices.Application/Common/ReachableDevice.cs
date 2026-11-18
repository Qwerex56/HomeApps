using Devices.Domain.Devices;

namespace Devices.Application.Common;

/// <summary>A device JARVIS may use for the caller, with the slug that names its tools (<see cref="Slug"/> differs from the device's own only after a collision across scopes).</summary>
internal sealed record ReachableDevice(Device Device, string Slug)
{
    public string ToolName(string action) => $"{Slug}_{action}";
}

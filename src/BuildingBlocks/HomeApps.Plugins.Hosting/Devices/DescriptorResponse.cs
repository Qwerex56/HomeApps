using System.Text.Json;
using HomeApps.Plugins.Devices;

namespace HomeApps.Plugins.Hosting.Devices;

internal sealed record DescriptorResponse(string? Model, string[]? Roles, string[]? Capabilities);

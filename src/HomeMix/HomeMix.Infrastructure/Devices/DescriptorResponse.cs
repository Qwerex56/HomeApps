namespace HomeMix.Infrastructure.Devices;

internal sealed record DescriptorResponse(string? Model, string[]? Roles, string[]? Capabilities);

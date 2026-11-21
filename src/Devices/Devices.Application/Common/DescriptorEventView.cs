namespace Devices.Application.Common;

/// <summary>An event of a device's descriptor as the household tab shows it.</summary>
public sealed record DescriptorEventView(string Name, string? Label, bool Notify, bool IsLocation);

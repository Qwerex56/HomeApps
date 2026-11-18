namespace Auth.Application.Devices;

/// <summary>The device's secret, handed out once; Auth keeps only its hash.</summary>
public sealed record PairedDeviceView(Guid DeviceId, string Secret);

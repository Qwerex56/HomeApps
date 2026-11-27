using System.Text.Json;

namespace Devices.Application.Common;

/// <summary>A device's last values with the time they were read, and its health from the last status report.</summary>
public sealed record DeviceStateView(
    Guid DeviceId,
    JsonElement Values,
    DateTimeOffset? ValuesAt,
    bool Online,
    bool Silent,
    bool LowBattery,
    DateTimeOffset? LastStatusAt,
    double? Battery,
    double? Voltage,
    int? Rssi,
    long? Uptime,
    string? Firmware,
    string? Error,
    JsonElement? PendingSettings = null);

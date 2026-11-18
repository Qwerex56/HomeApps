using System.Text.Json;

namespace Devices.Application.Frames;

/// <summary>
/// A frame a device sends, on <c>/devices/link</c> or in the array of <c>POST /devices/report</c>: <c>state</c> (<paramref name="Values"/>),
/// <c>event</c> (<paramref name="Name"/>, <paramref name="At"/>, <paramref name="Data"/>) or <c>status</c> (the heartbeat fields).
/// </summary>
public sealed record DeviceFrame(
    string? Type,
    JsonElement? Values = null,
    string? Name = null,
    string? At = null,
    JsonElement? Data = null,
    double? Battery = null,
    double? Voltage = null,
    int? Rssi = null,
    long? Uptime = null,
    string? Firmware = null,
    string? Error = null);

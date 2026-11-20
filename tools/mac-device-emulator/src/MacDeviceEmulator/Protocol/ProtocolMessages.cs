using System.Text.Json;
using System.Text.Json.Serialization;

namespace MacDeviceEmulator.Protocol;

public sealed record DeviceFrame(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("values")] JsonElement? Values = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("at")] string? At = null,
    [property: JsonPropertyName("data")] JsonElement? Data = null,
    [property: JsonPropertyName("battery")] double? Battery = null,
    [property: JsonPropertyName("voltage")] double? Voltage = null,
    [property: JsonPropertyName("rssi")] int? Rssi = null,
    [property: JsonPropertyName("uptime")] long? Uptime = null,
    [property: JsonPropertyName("firmware")] string? Firmware = null,
    [property: JsonPropertyName("error")] string? Error = null);

public sealed record FrameResult(
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("text")] string? Text = null);

public sealed record LinkMessage(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("device")] string? Device = null,
    [property: JsonPropertyName("kind")] string? Kind = null,
    [property: JsonPropertyName("actions")] IReadOnlyList<LinkAction>? Actions = null,
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("input")] JsonElement? Input = null,
    [property: JsonPropertyName("ok")] bool? Ok = null,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("deviceId")] Guid? DeviceId = null,
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("descriptor")] JsonElement? Descriptor = null,
    [property: JsonPropertyName("value")] JsonElement? Value = null);

public sealed record LinkAction(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("input")] JsonElement? Input = null,
    [property: JsonPropertyName("readOnly")] bool ReadOnly = false,
    [property: JsonPropertyName("confirm")] bool Confirm = false);

using System.Text.Json;

namespace Devices.Api.Requests;

/// <summary>The code, and the device's <c>homeapps.device/1</c> descriptor as a JSON object (optional for an old satellite).</summary>
public sealed record PairDeviceRequest(string Code, JsonElement? Descriptor = null);

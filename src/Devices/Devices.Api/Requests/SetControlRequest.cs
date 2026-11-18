using System.Text.Json;

namespace Devices.Api.Requests;

public sealed record SetControlRequest(string? Control, JsonElement? Value);

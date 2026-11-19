using System.Text.Json;

namespace HomeMix.Api.Requests;

public sealed record SetDeviceRequest(string? Control, JsonElement Value);

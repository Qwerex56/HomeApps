using System.Text.Json.Serialization;

namespace MacDeviceEmulator.Protocol;

public sealed record VoiceActionView(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("tool")] string Tool,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("confirmBy")] DateTimeOffset? ConfirmBy = null);

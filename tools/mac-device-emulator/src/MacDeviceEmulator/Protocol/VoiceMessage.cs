using System.Text.Json.Serialization;

namespace MacDeviceEmulator.Protocol;

public sealed record VoiceMessage(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("conversationId")] Guid? ConversationId = null,
    [property: JsonPropertyName("householdId")] Guid? HouseholdId = null,
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("actions")] IReadOnlyList<VoiceActionView>? Actions = null,
    [property: JsonPropertyName("awaiting")] bool? Awaiting = null,
    [property: JsonPropertyName("cancelled")] bool? Cancelled = null,
    [property: JsonPropertyName("code")] string? Code = null,
    [property: JsonPropertyName("detail")] string? Detail = null);

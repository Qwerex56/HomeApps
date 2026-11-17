using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>One message from <c>/jarvis/voice</c>: its <c>type</c>, its JSON, and for <c>audio</c> the WAV that followed.</summary>
internal sealed record ServerEvent(string Type, JsonElement Json, byte[]? Wav = null)
{
    public string? Text(string property) =>
        Json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public bool Flag(string property) =>
        Json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;
}

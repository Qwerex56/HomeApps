namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// The brain (jarvis-desktop-rework 2.7): any OpenAI-compatible <c>/v1/chat/completions</c> endpoint with tool calling.
/// Without <see cref="BaseUrl"/> and <see cref="Model"/> JARVIS says he is switched off.
/// </summary>
public sealed class OpenAiChatOptions
{
    public const string SectionName = "Jarvis:OpenAi";

    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    /// <summary>At most this many model turns per request.</summary>
    public int MaxTurns { get; set; } = 8;

    /// <summary>The gateway as this process reaches itself; every tool call goes through it.</summary>
    public string GatewayUrl { get; set; } = "http://localhost:8080/jarvis/mcp";

    /// <summary>One model call (fail fast): longer and JARVIS says he is unavailable.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}

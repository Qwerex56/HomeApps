namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// The <c>ClaudeCode</c> brain (jarvis-v1.md 2.2, JV2): Claude Code without a window, signed in with the owner's plan
/// (<c>claude setup-token</c> on the Mac, the result in <c>JARVIS_CLAUDE_CODE_OAUTH_TOKEN</c>).
/// </summary>
public sealed class ClaudeCodeOptions
{
    public const string SectionName = "Jarvis:ClaudeCode";

    /// <summary>The CLI, on PATH in the image.</summary>
    public string Command { get; set; } = "claude";

    /// <summary>Given to the process as <c>CLAUDE_CODE_OAUTH_TOKEN</c>.</summary>
    public string OAuthToken { get; set; } = string.Empty;

    /// <summary>Empty: Claude Code's default.</summary>
    public string? Model { get; set; }

    /// <summary>At most this many model turns per request (2.1: up to 8 tool steps).</summary>
    public int MaxTurns { get; set; } = 8;

    /// <summary>The gateway as this process reaches itself; every tool call goes through it.</summary>
    public string GatewayUrl { get; set; } = "http://localhost:8080/jarvis/mcp";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
}

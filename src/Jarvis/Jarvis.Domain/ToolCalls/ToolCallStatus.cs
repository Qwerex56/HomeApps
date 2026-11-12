namespace Jarvis.Domain.ToolCalls;

/// <summary>The action card's statuses (jarvis-v1.md 2.1, 5.4).</summary>
public enum ToolCallStatus
{
    Done = 0,
    Failed = 1,

    /// <summary>A destructive tool waits for the person who asked (JV9), 10 minutes.</summary>
    NeedsConfirmation = 2,
    Denied = 3,
    Expired = 4,
}

using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Common;

/// <summary>One line of the action card (jarvis-v1.md 5.4).</summary>
/// <param name="Description">What happened in words: the tool's own answer, or what waits for a confirmation.</param>
public sealed record ActionView(Guid Id, string Tool, string Description, ToolCallStatus Status, DateTimeOffset? ConfirmBy)
{
    public static ActionView From(ToolCall call, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(call);
        var status = call.StatusAt(now);
        var description = status is ToolCallStatus.NeedsConfirmation or ToolCallStatus.Expired or ToolCallStatus.Denied
            ? $"{call.Tool} {call.Arguments}".Trim()
            : call.Result ?? call.Tool;
        return new ActionView(call.Id, call.Tool, description, status, status == ToolCallStatus.NeedsConfirmation ? call.ConfirmBy : null);
    }
}

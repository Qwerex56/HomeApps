using FluentResults;

namespace Jarvis.Domain.ToolCalls;

/// <summary>
/// One tool JARVIS called in a run, as the gateway saw it: the tool, its arguments, what it answered. A destructive one
/// is not called at once: it waits <see cref="ConfirmationWindow"/> for the person who asked (JV9) and is called only
/// after they confirm. A read is logged too but is not an action on the card.
/// </summary>
public sealed class ToolCall
{
    public static readonly TimeSpan ConfirmationWindow = TimeSpan.FromMinutes(10);

    private ToolCall() { }

    public Guid Id { get; private set; }

    public Guid RunId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>The name at the gateway, e.g. <c>todo_add_task</c>.</summary>
    public string Tool { get; private set; } = string.Empty;

    /// <summary>JSON as the model sent it; erased after 7 days (JV12).</summary>
    public string? Arguments { get; private set; }

    /// <summary>The tool's answer, plain text a person can read.</summary>
    public string? Result { get; private set; }

    public bool ReadOnly { get; private set; }

    public ToolCallStatus Status { get; private set; }

    public DateTimeOffset CalledAt { get; private set; }

    /// <summary>Only while it waits for a confirmation.</summary>
    public DateTimeOffset? ConfirmBy { get; private set; }

    public static ToolCall Record(Guid runId, Guid userId, string tool, string? arguments, string result, bool failed, bool readOnly, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        var call = New(runId, userId, tool, arguments, time);
        call.Result = result;
        call.ReadOnly = readOnly;
        call.Status = failed ? ToolCallStatus.Failed : ToolCallStatus.Done;
        return call;
    }

    public static ToolCall AwaitConfirmation(Guid runId, Guid userId, string tool, string? arguments, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        var call = New(runId, userId, tool, arguments, time);
        call.Status = ToolCallStatus.NeedsConfirmation;
        call.ConfirmBy = call.CalledAt + ConfirmationWindow;
        return call;
    }

    /// <summary>The status a person sees now: a confirmation not given in time has expired.</summary>
    public ToolCallStatus StatusAt(DateTimeOffset now) =>
        Status == ToolCallStatus.NeedsConfirmation && ConfirmBy <= now ? ToolCallStatus.Expired : Status;

    /// <summary>May the person who asked confirm or deny it now? The caller then runs the tool and calls <see cref="Complete"/>.</summary>
    public FluentResults.Result CanDecide(Guid userId, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (userId != UserId)
        {
            return FluentResults.Result.Fail(ToolCallErrors.NotFound);
        }

        return StatusAt(time.GetUtcNow()) switch
        {
            ToolCallStatus.NeedsConfirmation => FluentResults.Result.Ok(),
            ToolCallStatus.Expired => FluentResults.Result.Fail(ToolCallErrors.Expired),
            _ => FluentResults.Result.Fail(ToolCallErrors.NotPending),
        };
    }

    public void Complete(string result, bool failed)
    {
        Result = result;
        Status = failed ? ToolCallStatus.Failed : ToolCallStatus.Done;
        ConfirmBy = null;
    }

    public void Deny()
    {
        Status = ToolCallStatus.Denied;
        ConfirmBy = null;
    }

    /// <summary>JV12: after 7 days the tool and the status remain, not what was sent or answered.</summary>
    public void EraseContent()
    {
        Arguments = null;
        Result = null;
    }

    private static ToolCall New(Guid runId, Guid userId, string tool, string? arguments, TimeProvider time)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tool);
        var now = time.GetUtcNow();
        return new ToolCall
        {
            Id = Guid.CreateVersion7(now),
            RunId = runId,
            UserId = userId,
            Tool = tool,
            Arguments = arguments,
            CalledAt = now,
        };
    }
}

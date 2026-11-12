namespace Jarvis.Domain.Runs;

/// <summary>
/// One request to JARVIS (jarvis-v1.md 2.1, 2.5): who asked, through which channel, in which household, what, and what
/// came back with its cost. The tool calls are <see cref="ToolCalls.ToolCall"/>s of their own, written by the gateway
/// while the run goes on. The texts are kept 7 days and the rest 30 (JV12).
/// </summary>
public sealed class AssistantRun
{
    private AssistantRun() { }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary><c>app</c> (the account's conversation and <c>ask</c>), <c>chat</c>, later voice and the hub.</summary>
    public string Channel { get; private set; } = string.Empty;

    /// <summary>The channel's own pointer, e.g. <c>{conversationId}/{messageId}</c> in the chat.</summary>
    public string? Reference { get; private set; }

    public Guid? HouseholdId { get; private set; }

    public string? Prompt { get; private set; }

    public string? Response { get; private set; }

    public RunStatus Status { get; private set; }

    public string? Model { get; private set; }

    /// <summary>As the provider reports it; with Claude Code an estimate, since the plan sends no bill (JV11).</summary>
    public decimal? CostUsd { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public static AssistantRun Start(Guid userId, string channel, string? reference, Guid? householdId, string prompt, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A run needs the person who asked.", nameof(userId));
        }

        var now = time.GetUtcNow();
        return new AssistantRun
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            Channel = channel,
            Reference = reference,
            HouseholdId = householdId,
            Prompt = prompt,
            Status = RunStatus.Running,
            StartedAt = now,
        };
    }

    /// <summary>A run that never reached the model; <paramref name="answer"/> is what the person is told.</summary>
    public static AssistantRun Refuse(Guid userId, string channel, string? reference, Guid? householdId, string prompt, string answer, TimeProvider time)
    {
        var run = Start(userId, channel, reference, householdId, prompt, time);
        run.Status = RunStatus.Refused;
        run.Response = answer;
        run.FinishedAt = run.StartedAt;
        return run;
    }

    public bool IsRunning => Status == RunStatus.Running;

    public void Finish(string response, string? model, decimal? costUsd, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (!IsRunning)
        {
            return;
        }

        Status = RunStatus.Finished;
        Response = response;
        Model = model;
        CostUsd = costUsd;
        FinishedAt = time.GetUtcNow();
    }

    public void Fail(string response, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (!IsRunning)
        {
            return;
        }

        Status = RunStatus.Failed;
        Response = response;
        FinishedAt = time.GetUtcNow();
    }

    /// <summary>JV12: after 7 days only that it happened, when and at what cost remain.</summary>
    public void EraseContent()
    {
        Prompt = null;
        Response = null;
    }
}

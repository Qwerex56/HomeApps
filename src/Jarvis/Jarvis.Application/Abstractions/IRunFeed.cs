namespace Jarvis.Application.Abstractions;

/// <summary>
/// What a run does while it works, for whoever watches it live (the account's conversation over SSE): pieces of text
/// from the brain, actions from the gateway. Nothing is kept: with nobody watching an update is dropped.
/// </summary>
public interface IRunFeed
{
    /// <summary>The run started for a stream someone opened before asking.</summary>
    void Link(Guid streamId, Guid runId);

    void Publish(Guid runId, RunUpdate update);
}

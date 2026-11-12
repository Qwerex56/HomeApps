namespace Jarvis.Application.Abstractions;

/// <summary>
/// Who thinks (jarvis-v1.md 2.2): Claude Code on the owner's plan in v1, an API later. It gets the request and a token
/// for the gateway, calls tools only through the gateway (which logs them and holds destructive ones back) and streams
/// its answer.
/// </summary>
public interface IAssistantBrain
{
    /// <summary>False when it cannot think at all (no subscription token configured): JARVIS then says he is switched off.</summary>
    bool IsReady { get; }

    IAsyncEnumerable<BrainEvent> RunAsync(BrainRequest request, CancellationToken cancellationToken);
}

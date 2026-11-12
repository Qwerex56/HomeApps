namespace Jarvis.Application.Abstractions;

/// <summary>Calls a tool of the gateway for a person outside a model's run: a confirmed action (JV9).</summary>
public interface IToolExecutor
{
    Task<ToolOutcome> CallAsync(Guid userId, string tool, string? arguments, CancellationToken cancellationToken);
}

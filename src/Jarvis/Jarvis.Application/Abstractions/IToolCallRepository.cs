using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.Abstractions;

public interface IToolCallRepository
{
    Task<ToolCall?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<ToolCall>> ListForRunAsync(Guid runId, CancellationToken cancellationToken);

    void Add(ToolCall toolCall);
}

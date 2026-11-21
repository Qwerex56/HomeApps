using Jarvis.Application.Abstractions;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Api.Tests;

internal sealed class FakeToolCallRepository : IToolCallRepository
{
    public List<ToolCall> Calls { get; } = [];

    public Task<ToolCall?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Calls.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<ToolCall>> ListForRunAsync(Guid runId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ToolCall>>([.. Calls.Where(c => c.RunId == runId).OrderBy(c => c.CalledAt)]);

    public Task<IReadOnlyList<ToolCall>> ListForRunsAsync(IReadOnlyCollection<Guid> runIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ToolCall>>([.. Calls.Where(c => runIds.Contains(c.RunId)).OrderBy(c => c.CalledAt)]);

    public Task<IReadOnlyList<ToolCall>> ListPendingForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ToolCall>>([.. Calls
            .Where(c => c.UserId == userId && c.Status == ToolCallStatus.NeedsConfirmation)
            .OrderByDescending(c => c.CalledAt)]);

    public void Add(ToolCall toolCall) => Calls.Add(toolCall);
}

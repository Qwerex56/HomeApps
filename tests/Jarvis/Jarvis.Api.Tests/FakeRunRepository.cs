using Jarvis.Application.Abstractions;
using Jarvis.Domain.Runs;

namespace Jarvis.Api.Tests;

internal sealed class FakeRunRepository : IRunRepository
{
    public List<AssistantRun> Runs { get; } = [];

    public Task<AssistantRun?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Runs.FirstOrDefault(r => r.Id == id));

    public Task<decimal> CostSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult(Runs.Where(r => r.StartedAt >= since).Sum(r => r.CostUsd ?? 0m));

    public void Add(AssistantRun run) => Runs.Add(run);

    public Task ApplyRetentionAsync(DateTimeOffset contentBefore, DateTimeOffset rowsBefore, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        Runs.RemoveAll(r => r.UserId == userId);
        return Task.CompletedTask;
    }
}

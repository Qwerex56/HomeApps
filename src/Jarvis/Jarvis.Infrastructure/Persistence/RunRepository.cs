using Jarvis.Application.Abstractions;
using Jarvis.Domain.Runs;
using Microsoft.EntityFrameworkCore;

namespace Jarvis.Infrastructure.Persistence;

internal sealed class RunRepository(JarvisDbContext db) : IRunRepository
{
    public Task<AssistantRun?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Runs.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<decimal> CostSinceAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        await db.Runs.Where(r => r.StartedAt >= since && r.CostUsd != null).SumAsync(r => r.CostUsd!.Value, cancellationToken);

    public void Add(AssistantRun run) => db.Runs.Add(run);

    public async Task ApplyRetentionAsync(DateTimeOffset contentBefore, DateTimeOffset rowsBefore, CancellationToken cancellationToken)
    {
        await db.Runs.Where(r => r.StartedAt < rowsBefore).ExecuteDeleteAsync(cancellationToken);
        await db.Runs.Where(r => r.StartedAt < contentBefore && (r.Prompt != null || r.Response != null))
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.Prompt, (string?)null).SetProperty(r => r.Response, (string?)null), cancellationToken);
        await db.ToolCalls.Where(c => c.CalledAt < contentBefore && (c.Arguments != null || c.Result != null))
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Arguments, (string?)null).SetProperty(c => c.Result, (string?)null), cancellationToken);
    }

    public Task ForgetAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Runs.Where(r => r.UserId == userId).ExecuteDeleteAsync(cancellationToken);
}

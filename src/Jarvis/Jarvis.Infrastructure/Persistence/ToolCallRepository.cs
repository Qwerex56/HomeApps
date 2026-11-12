using Jarvis.Application.Abstractions;
using Jarvis.Domain.ToolCalls;
using Microsoft.EntityFrameworkCore;

namespace Jarvis.Infrastructure.Persistence;

internal sealed class ToolCallRepository(JarvisDbContext db) : IToolCallRepository
{
    public Task<ToolCall?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.ToolCalls.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ToolCall>> ListForRunAsync(Guid runId, CancellationToken cancellationToken) =>
        await db.ToolCalls.Where(c => c.RunId == runId).OrderBy(c => c.CalledAt).ToListAsync(cancellationToken);

    public void Add(ToolCall toolCall) => db.ToolCalls.Add(toolCall);
}

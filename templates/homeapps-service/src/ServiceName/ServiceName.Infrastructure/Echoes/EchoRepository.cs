using ServiceName.Application.Abstractions;
using ServiceName.Domain.Echoes;
using ServiceName.Infrastructure.Persistence;

namespace ServiceName.Infrastructure.Echoes;

internal sealed class EchoRepository(ServiceNameDbContext db) : IEchoRepository
{
    public async Task<Echo?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Echoes.FindAsync([id], cancellationToken);

    public void Add(Echo echo) => db.Echoes.Add(echo);
}

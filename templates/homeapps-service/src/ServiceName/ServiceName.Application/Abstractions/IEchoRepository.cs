using ServiceName.Domain.Echoes;

namespace ServiceName.Application.Abstractions;

public interface IEchoRepository
{
    Task<Echo?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Echo echo);
}

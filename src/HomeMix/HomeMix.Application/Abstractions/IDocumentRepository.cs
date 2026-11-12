using HomeMix.Domain.Data;

namespace HomeMix.Application.Abstractions;

public interface IDocumentRepository
{
    Task<AddonDocument?> FindAsync(DataSpace space, string collection, string key, CancellationToken cancellationToken);

    /// <summary>A collection's documents, by key.</summary>
    Task<IReadOnlyList<AddonDocument>> ListAsync(DataSpace space, string collection, CancellationToken cancellationToken);

    Task<int> CountAsync(DataSpace space, string collection, CancellationToken cancellationToken);

    /// <summary>Bytes the installation's documents take, in both scopes.</summary>
    Task<long> UsedBytesAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Every document of these installations, in both scopes.</summary>
    Task<IReadOnlyList<AddonDocument>> ListOfAsync(IReadOnlyList<Guid> installationIds, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes, at once, the documents of installations removed before <paramref name="removedBefore"/> and of
    /// installations that do not exist; returns how many.
    /// </summary>
    Task<int> DeleteUnreachableAsync(DateTimeOffset removedBefore, CancellationToken cancellationToken);

    void Add(AddonDocument document);

    void Remove(AddonDocument document);
}

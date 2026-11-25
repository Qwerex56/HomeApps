namespace Accounts.Application.Abstractions;

/// <summary>Where uploaded avatars live: one file per user (release 3.0, 2.15).</summary>
public interface IAvatarStore
{
    Task SaveAsync(Guid userId, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    /// <summary>The file's bytes, or null when the user has no uploaded avatar.</summary>
    Task<byte[]?> ReadAsync(Guid userId, CancellationToken cancellationToken);
}

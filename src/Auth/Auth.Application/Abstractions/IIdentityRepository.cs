using Auth.Domain.Common;
using Auth.Domain.Identities;

namespace Auth.Application.Abstractions;

/// <summary>Loads and stores <see cref="Identity"/> with its sessions, tokens and external logins. Saving is the unit of work's job.</summary>
public interface IIdentityRepository
{
    Task<Identity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Identity?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<Identity?> GetByExternalLoginAsync(ExternalProvider provider, string subject, CancellationToken cancellationToken);

    /// <summary>The identity owning a session with a refresh token of this hash, used or not.</summary>
    Task<Identity?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>The identity whose pending email link has this hash.</summary>
    Task<Identity?> GetByEmailTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);

    Task<bool> ExternalLoginExistsAsync(ExternalProvider provider, string subject, CancellationToken cancellationToken);

    Task<bool> OwnerExistsAsync(CancellationToken cancellationToken);

    void Add(Identity identity);

    /// <summary>Hard delete with logins, sessions and tokens (A-32, "forget me" only).</summary>
    void Remove(Identity identity);
}

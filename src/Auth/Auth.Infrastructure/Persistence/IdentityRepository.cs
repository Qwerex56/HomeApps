using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

/// <summary>Always loads the whole aggregate: logins, sessions and their tokens. The daily cleanup keeps it small.</summary>
internal sealed class IdentityRepository(AuthDbContext db) : IIdentityRepository
{
    private IQueryable<Identity> Aggregates =>
        db.Identities.Include(i => i.Sessions).ThenInclude(s => s.RefreshTokens).AsSplitQuery();

    public Task<Identity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Aggregates.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<Identity?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Aggregates.FirstOrDefaultAsync(i => i.Email == email, cancellationToken);

    public Task<Identity?> GetByExternalLoginAsync(ExternalProvider provider, string subject, CancellationToken cancellationToken) =>
        Aggregates.FirstOrDefaultAsync(
            i => i.ExternalLogins.Any(l => l.Provider == provider && l.Subject == subject), cancellationToken);

    public Task<Identity?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Aggregates.FirstOrDefaultAsync(
            i => i.Sessions.Any(s => s.RefreshTokens.Any(t => t.TokenHash == tokenHash)), cancellationToken);

    public Task<Identity?> GetByEmailTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Aggregates.FirstOrDefaultAsync(i => i.EmailToken != null && i.EmailToken.TokenHash == tokenHash, cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        db.Identities.AnyAsync(i => i.Email == email, cancellationToken);

    public Task<bool> ExternalLoginExistsAsync(ExternalProvider provider, string subject, CancellationToken cancellationToken) =>
        db.Identities.AnyAsync(i => i.ExternalLogins.Any(l => l.Provider == provider && l.Subject == subject), cancellationToken);

    public Task<bool> OwnerExistsAsync(CancellationToken cancellationToken) =>
        db.Identities.AnyAsync(i => i.Role == ServerRole.Owner, cancellationToken);

    public void Add(Identity identity) => db.Identities.Add(identity);

    public void Remove(Identity identity) => db.Identities.Remove(identity);
}

using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

internal sealed class AuthorizationCodeRepository(AuthDbContext db) : IAuthorizationCodeRepository
{
    public Task<AuthorizationCode?> GetByHashAsync(string codeHash, CancellationToken cancellationToken) =>
        db.AuthorizationCodes.FirstOrDefaultAsync(c => c.CodeHash == codeHash, cancellationToken);

    public void Add(AuthorizationCode code) => db.AuthorizationCodes.Add(code);
}

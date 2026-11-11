using Auth.Application.Abstractions;
using Auth.Domain.ConnectedApps;

namespace Auth.Application.Tests;

internal sealed class InMemoryAuthorizationCodes : IAuthorizationCodeRepository
{
    public List<AuthorizationCode> Items { get; } = [];

    public Task<AuthorizationCode?> GetByHashAsync(string codeHash, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(c => c.CodeHash == codeHash));

    public void Add(AuthorizationCode code) => Items.Add(code);
}

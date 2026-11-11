using Auth.Domain.ConnectedApps;

namespace Auth.Application.Abstractions;

public interface IAuthorizationCodeRepository
{
    /// <summary>The code with this hash, used or not, until the daily cleanup removes it.</summary>
    Task<AuthorizationCode?> GetByHashAsync(string codeHash, CancellationToken cancellationToken);

    void Add(AuthorizationCode code);
}

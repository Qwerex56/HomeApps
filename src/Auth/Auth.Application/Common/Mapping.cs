using Auth.Domain.Identities;
using Contracts = HomeApps.Contracts.Auth;
using DomainRole = Auth.Domain.Common.ServerRole;

namespace Auth.Application.Common;

/// <summary>Domain enums to their contract twins, spelled out so a renamed member breaks the build, not a consumer.</summary>
internal static class Mapping
{
    public static Contracts.ServerRole ToContract(this DomainRole role) => role switch
    {
        DomainRole.User => Contracts.ServerRole.User,
        DomainRole.Admin => Contracts.ServerRole.Admin,
        DomainRole.Owner => Contracts.ServerRole.Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    public static Contracts.RegistrationSource ToContract(this RegistrationSource source) => source switch
    {
        RegistrationSource.Password => Contracts.RegistrationSource.Password,
        RegistrationSource.Google => Contracts.RegistrationSource.Google,
        RegistrationSource.Apple => Contracts.RegistrationSource.Apple,
        RegistrationSource.Bootstrap => Contracts.RegistrationSource.Bootstrap,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };

    /// <summary>No method means the session was only refreshed.</summary>
    public static Contracts.SignInMethod ToContract(this AuthMethod? method) => method switch
    {
        null => Contracts.SignInMethod.Refresh,
        AuthMethod.Password => Contracts.SignInMethod.Password,
        AuthMethod.Google => Contracts.SignInMethod.Google,
        AuthMethod.Apple => Contracts.SignInMethod.Apple,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
    };
}

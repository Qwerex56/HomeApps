using Accounts.Domain.Users;
using Contracts = HomeApps.Contracts.Auth;
using DomainRole = Accounts.Domain.Common.ServerRole;

namespace Accounts.Application.Common;

/// <summary>Contract enums to their domain twins, spelled out so a renamed member breaks the build, not a consumer.</summary>
internal static class Mapping
{
    public static DomainRole ToDomain(this Contracts.ServerRole role) => role switch
    {
        Contracts.ServerRole.User => DomainRole.User,
        Contracts.ServerRole.Admin => DomainRole.Admin,
        Contracts.ServerRole.Owner => DomainRole.Owner,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    public static RegistrationSource ToDomain(this Contracts.RegistrationSource source) => source switch
    {
        Contracts.RegistrationSource.Password => RegistrationSource.Password,
        Contracts.RegistrationSource.Google => RegistrationSource.Google,
        Contracts.RegistrationSource.Apple => RegistrationSource.Apple,
        Contracts.RegistrationSource.Bootstrap => RegistrationSource.Bootstrap,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };
}

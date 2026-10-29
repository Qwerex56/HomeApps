namespace HomeApps.Contracts.Auth;

/// <summary>Server-wide role, owned by Auth and sent in the <c>role</c> claim. Ordered: a higher value is a higher role.</summary>
public enum ServerRole
{
    User = 0,
    Admin = 1,
    Owner = 2,
}

/// <summary>How an identity was created. <see cref="Bootstrap"/>: the first Owner, from configuration.</summary>
public enum RegistrationSource
{
    Password = 0,
    Google = 1,
    Apple = 2,
    Bootstrap = 3,
}

/// <summary>How the user proved who they are; <see cref="Refresh"/> when a session was only refreshed.</summary>
public enum SignInMethod
{
    Password = 0,
    Google = 1,
    Apple = 2,
    Refresh = 3,
}

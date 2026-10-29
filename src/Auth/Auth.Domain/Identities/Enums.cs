namespace Auth.Domain.Identities;

/// <summary>Copy of the account state owned by Accounts (A-32). Only <see cref="Active"/> can sign in.</summary>
public enum AccountStatus
{
    Active = 0,
    DisabledBySelf = 1,
    DisabledByAdmin = 2,
}

/// <summary>How the identity was created. <see cref="Bootstrap"/>: through the bootstrap invitation (A-30).</summary>
public enum RegistrationSource
{
    Password = 0,
    Google = 1,
    Apple = 2,
    Bootstrap = 3,
}

public enum ExternalProvider
{
    Google = 0,
    Apple = 1,
}

/// <summary>How the user proved who they are when a session started.</summary>
public enum AuthMethod
{
    Password = 0,
    Google = 1,
    Apple = 2,
}

public enum EmailTokenPurpose
{
    PasswordReset = 0,
    PasswordChange = 1,
    EmailChange = 2,
}

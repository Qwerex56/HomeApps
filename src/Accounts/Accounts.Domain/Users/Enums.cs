namespace Accounts.Domain.Users;

/// <summary>Active accounts sign in and use households; disabled ones keep all data and can come back (R5, R8).</summary>
public enum AccountStatus
{
    Active = 0,
    Disabled = 1,
}

/// <summary>How the account was created (copy of Auth's source). <see cref="Bootstrap"/>: the first Owner, from configuration.</summary>
public enum RegistrationSource
{
    Password = 0,
    Google = 1,
    Apple = 2,
    Bootstrap = 3,
}

/// <summary>What a history entry records.</summary>
public enum HistoryAction
{
    Registered = 0,
    ProfileUpdated = 1,
    Disabled = 2,
    Enabled = 3,
    ReactivatedBySelf = 4,
    RoleSynced = 5,
    EmailSynced = 6,
}

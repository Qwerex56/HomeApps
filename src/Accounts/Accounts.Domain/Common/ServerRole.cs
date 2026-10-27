namespace Accounts.Domain.Common;

/// <summary>
/// Copy of the server-wide role owned by Auth (A-23), kept for R1, the admin list and its filters.
/// Ordered: a higher value is a higher role. Household roles are a different thing and never live here.
/// </summary>
public enum ServerRole
{
    User = 0,
    Admin = 1,
    Owner = 2,
}

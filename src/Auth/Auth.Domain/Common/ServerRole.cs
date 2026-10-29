namespace Auth.Domain.Common;

/// <summary>
/// Server-wide role, owned by Auth and put in the <c>role</c> claim. Ordered: a higher value is a higher role.
/// Household roles are a different thing and never live here.
/// </summary>
public enum ServerRole
{
    User = 0,
    Admin = 1,
    Owner = 2,
}

namespace Households.Domain.Common;

/// <summary>
/// Copy of the server-wide role owned by Auth (claim <c>role</c>). In Households it only decides who is a server admin
/// (D8: reads every household, archives and restores). Ordered: a higher value is a higher role.
/// Household roles are a different thing (<see cref="Households.HouseholdRole"/>) and never reach the token.
/// </summary>
public enum ServerRole
{
    User = 0,
    Admin = 1,
    Owner = 2,
}

namespace HomeMix.Domain.Data;

/// <summary>
/// The two levels of an installation's space (docs/homemix.md, "Data"): <see cref="Household"/> is the installation's
/// shared data (on an account: that person's), <see cref="User"/> one person's private data in it.
/// </summary>
public enum DataScope
{
    Household = 0,
    User = 1,
}

namespace Households.Application.Common;

/// <summary>Configuration section <c>Households</c>. Registered with defaults; the Api binds it from configuration.</summary>
public sealed class HouseholdSettings
{
    public const string Section = "Households";

    /// <summary>D10: everyone counts, guests and inactive accounts too.</summary>
    public int MaxMembers { get; set; } = 100;
}

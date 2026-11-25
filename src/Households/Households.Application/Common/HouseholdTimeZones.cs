namespace Households.Application.Common;

/// <summary>
/// 2.11, Decision 5: the effective zone is the household's own, else the owner's account zone, else
/// <see cref="Fallback"/>. A household without its own zone follows whoever owns it now.
/// </summary>
public static class HouseholdTimeZones
{
    public const string Fallback = "Europe/Warsaw";
    public const string FallbackSource = "fallback";

    public static (string TimeZone, string Source) Resolve(string? household, string? owner) =>
        !string.IsNullOrEmpty(household) ? (household, "household")
        : !string.IsNullOrEmpty(owner) ? (owner, "owner")
        : (Fallback, FallbackSource);
}

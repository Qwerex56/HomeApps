namespace Jarvis.Application.Gateway;

/// <summary>The household's JARVIS settings from Households, kept for a short while; a household that cannot be read is <see cref="HouseholdCeiling.Closed"/>.</summary>
public interface IHouseholdCeilings
{
    Task<HouseholdCeiling> GetAsync(Guid householdId, CancellationToken cancellationToken);
}

namespace HomeMix.Application.Abstractions;

/// <summary>Households asked as a service (no person's token): who the admins are now. Throws when Households cannot be reached.</summary>
public interface IHouseholdAdmins
{
    /// <summary>The people who are Admin or Owner of the household.</summary>
    Task<IReadOnlyList<Guid>> GetAdminIdsAsync(Guid householdId, CancellationToken cancellationToken);
}

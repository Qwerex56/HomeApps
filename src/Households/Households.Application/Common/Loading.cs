using Households.Application.Abstractions;
using Households.Domain.Households;
using FluentResults;

namespace Households.Application.Common;

internal static class Loading
{
    /// <summary>The household or <c>household.not_found</c>; the domain then decides what the caller may do (Q3).</summary>
    public static async Task<Result<Household>> FindAsync(this IHouseholdRepository households, Guid id, CancellationToken cancellationToken)
    {
        var household = await households.GetByIdAsync(id, cancellationToken);
        return household is null ? Result.Fail<Household>(HouseholdErrors.NotFound) : household;
    }

    /// <summary>Q5: no copy yet is fine (the token is enough), a disabled copy is not.</summary>
    public static async Task<Result> EnsureActiveAsync(this IUserCopies users, Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken);
        return user is { IsActive: false } ? Result.Fail(ApplicationErrors.AccountDisabled) : Result.Ok();
    }

    /// <summary>D5, D6: whether each member's account is active; members without a copy yet count as active.</summary>
    public static async Task<Func<Guid, bool>> ActivityOfAsync(
        this IUserCopies users, IEnumerable<Household> households, CancellationToken cancellationToken)
    {
        var ids = households.SelectMany(h => h.Members).Select(m => m.UserId).Distinct().ToList();
        var copies = await users.GetManyAsync(ids, cancellationToken);
        return id => !copies.TryGetValue(id, out var copy) || copy.IsActive;
    }
}

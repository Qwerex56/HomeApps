using FluentResults;
using HomeApps.BuildingBlocks.Mcp;

namespace HomeApps.Plugins.Hosting;

/// <summary>For MCP tools: a household or a person by name or id, the way <see cref="NameLookup"/> finds them.</summary>
public static class HouseholdDirectoryExtensions
{
    /// <summary>One of the caller's households; <c>household.not_found</c> or <c>household.ambiguous</c> otherwise.</summary>
    public static async Task<Result<HouseholdName>> FindHouseholdAsync(
        this IHouseholdDirectory directory, string? household, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(directory);
        var mine = await directory.MyHouseholdsAsync(cancellationToken).ConfigureAwait(false);
        return NameLookup.Find(mine, household, h => h.Id, h => h.Name, "household", "households_my_households");
    }

    /// <summary>
    /// Where a tool looks when a household may be left out: the one named, or every household of the caller, so
    /// "my tasks" needs no household. Skip those where <see cref="PluginErrors.IsOffOrForbidden"/> holds.
    /// </summary>
    public static async Task<Result<IReadOnlyList<HouseholdName>>> HouseholdsToSearchAsync(
        this IHouseholdDirectory directory, string? household, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(directory);
        if (!string.IsNullOrWhiteSpace(household))
        {
            var found = await directory.FindHouseholdAsync(household, cancellationToken).ConfigureAwait(false);
            return found.IsFailed ? found.ToResult<IReadOnlyList<HouseholdName>>() : Result.Ok<IReadOnlyList<HouseholdName>>([found.Value]);
        }

        var mine = await directory.MyHouseholdsAsync(cancellationToken).ConfigureAwait(false);
        return mine.Count == 0
            ? Result.Fail(LookupErrors.NotFound("household", "The person does not belong to any household."))
            : Result.Ok(mine);
    }

    /// <summary>Someone with access to the household; <c>person.not_found</c> or <c>person.ambiguous</c> otherwise.</summary>
    public static async Task<Result<MemberName>> FindPersonAsync(
        this IHouseholdDirectory directory, Guid householdId, string? person, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(directory);
        var people = await directory.MemberNamesAsync(householdId, cancellationToken).ConfigureAwait(false);
        return NameLookup.Find(people, person, p => p.UserId, p => p.Name, "person", "households_household_members");
    }
}

using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Households.Application.Households;
using MediatR;

namespace Households.Api.Mcp;

/// <summary>A model names a household the way a person does: by its name, or by the id a tool gave it before.</summary>
internal static class HouseholdLookup
{
    public static AppError NotFound { get; } = new(
        "household.not_found", "No household of the person has this name or id; my_households lists them.", ErrorKind.NotFound);

    public static AppError Ambiguous { get; } = new(
        "household.ambiguous", "Several households of the person have this name; use the id from my_households.", ErrorKind.Conflict);

    public static async Task<Result<Guid>> ResolveAsync(ISender sender, string household, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(household, out var id))
        {
            return id;
        }

        var mine = await sender.Send(new GetMyHouseholdsQuery(), cancellationToken);
        if (mine.IsFailed)
        {
            return mine.ToResult<Guid>();
        }

        var matches = mine.Value
            .Where(h => string.Equals(h.Name.Trim(), household.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
        return matches.Count switch
        {
            1 => matches[0].Id,
            0 => Result.Fail<Guid>(NotFound),
            _ => Result.Fail<Guid>(Ambiguous),
        };
    }
}

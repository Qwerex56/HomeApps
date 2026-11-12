using FluentResults;
using HomeApps.BuildingBlocks.Mcp;
using Households.Application.Households;
using MediatR;

namespace Households.Api.Mcp;

/// <summary>A model names a household the way a person does: by its name, or by the id a tool gave it before.</summary>
internal static class HouseholdLookup
{
    public static async Task<Result<Guid>> ResolveAsync(ISender sender, string household, CancellationToken cancellationToken)
    {
        var mine = await sender.Send(new GetMyHouseholdsQuery(), cancellationToken);
        if (mine.IsFailed)
        {
            return mine.ToResult<Guid>();
        }

        var found = NameLookup.Find(mine.Value, household, h => h.Id, h => h.Name, "household", "my_households");
        return found.IsSuccess ? found.Value.Id : found.ToResult<Guid>();
    }
}

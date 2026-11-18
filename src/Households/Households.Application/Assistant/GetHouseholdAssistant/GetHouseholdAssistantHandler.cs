using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;

namespace Households.Application.Assistant.GetHouseholdAssistant;

internal sealed class GetHouseholdAssistantHandler(IHouseholdRepository households)
    : IQueryHandler<GetHouseholdAssistantQuery, AssistantView>
{
    public async Task<Result<AssistantView>> Handle(GetHouseholdAssistantQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<AssistantView>();
        }

        return found.Value.Status == HouseholdStatus.Archived
            ? Result.Fail<AssistantView>(HouseholdErrors.NotFound)
            : AssistantView.From(found.Value.Assistant);
    }
}

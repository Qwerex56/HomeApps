using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Application.Abstractions;
using Households.Application.Common;

namespace Households.Application.Assistant.GetAssistant;

internal sealed class GetAssistantHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<GetAssistantQuery, AssistantView>
{
    public async Task<Result<AssistantView>> Handle(GetAssistantQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<AssistantView>();
        }

        var assistant = found.Value.ViewAssistant(currentUser.Actor(), time.GetUtcNow());
        return assistant.IsFailed ? assistant.ToResult<AssistantView>() : AssistantView.From(assistant.Value);
    }
}

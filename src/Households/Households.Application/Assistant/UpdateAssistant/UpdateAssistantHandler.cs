using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Application.Abstractions;
using Households.Application.Common;

namespace Households.Application.Assistant.UpdateAssistant;

internal sealed class UpdateAssistantHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UpdateAssistantCommand, AssistantView>
{
    public async Task<Result<AssistantView>> Handle(UpdateAssistantCommand command, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(command.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<AssistantView>();
        }

        var household = found.Value;
        var updated = household.UpdateAssistant(
            currentUser.Actor(), command.Enabled, command.Name, command.AccessLevel, command.ToolSources,
            command.EventDays, command.LocationEventDays, time);
        return updated.IsFailed ? updated.ToResult<AssistantView>() : AssistantView.From(household.Assistant);
    }
}

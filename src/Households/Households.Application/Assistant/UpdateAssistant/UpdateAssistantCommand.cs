using HomeApps.BuildingBlocks.Application.Messaging;
using Households.Domain.Households;

namespace Households.Application.Assistant.UpdateAssistant;

/// <summary>D2, D6, D9: the Owner or an Admin replaces the assistant's settings; returns the result.</summary>
public sealed record UpdateAssistantCommand(
    Guid HouseholdId, bool Enabled, string? Name, AssistantAccessLevel AccessLevel, AssistantToolSources ToolSources,
    int EventDays, int LocationEventDays) : ICommand<AssistantView>;

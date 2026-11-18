using Households.Domain.Households;

namespace Households.Application.Assistant;

/// <summary>The household's assistant as the settings tab shows it (D2, D9).</summary>
public sealed record AssistantView(
    bool Enabled, string Name, AssistantAccessLevel AccessLevel, AssistantToolSources ToolSources, int EventDays, int LocationEventDays)
{
    internal static AssistantView From(HouseholdAssistant a) =>
        new(a.Enabled, a.Name, a.AccessLevel, a.ToolSources, a.EventDays, a.LocationEventDays);
}

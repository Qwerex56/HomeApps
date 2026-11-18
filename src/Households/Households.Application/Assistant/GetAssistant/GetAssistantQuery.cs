using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Assistant.GetAssistant;

/// <summary>Anyone with access to the household, guests included (D6); a stranger gets 404 (Q3).</summary>
public sealed record GetAssistantQuery(Guid HouseholdId) : IQuery<AssistantView>;

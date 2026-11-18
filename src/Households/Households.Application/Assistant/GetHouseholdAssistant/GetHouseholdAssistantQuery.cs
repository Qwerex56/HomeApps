using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Assistant.GetHouseholdAssistant;

/// <summary>
/// H-2: the household's assistant for the JARVIS service, which filters its tools by it. No actor: the Api authorizes
/// the service client. Unknown and archived households are not found.
/// </summary>
public sealed record GetHouseholdAssistantQuery(Guid HouseholdId) : IQuery<AssistantView>;

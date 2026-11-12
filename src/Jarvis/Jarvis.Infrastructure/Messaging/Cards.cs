using HomeApps.Contracts.Assistant;
using Jarvis.Application.Common;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>An action of the card as the contracts carry it.</summary>
internal static class Cards
{
    public static AssistantAction Action(ActionView view) => new(
        view.Description,
        view.Status.ToString(),
        null,
        view.Status == ToolCallStatus.NeedsConfirmation ? view.Id : null,
        view.ConfirmBy);
}

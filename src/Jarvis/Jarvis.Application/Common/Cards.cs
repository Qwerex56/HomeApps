using HomeApps.Contracts.Assistant;

namespace Jarvis.Application.Common;

/// <summary>An action of the card as the contracts carry it.</summary>
public static class Cards
{
    /// <summary>The id stays whatever the status: a channel finds its card's row by it when the decision comes back.</summary>
    public static AssistantAction Action(ActionView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return new AssistantAction(view.Description, view.Status.ToString(), null, view.Id, view.ConfirmBy);
    }
}

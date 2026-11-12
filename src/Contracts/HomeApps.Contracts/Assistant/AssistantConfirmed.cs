namespace HomeApps.Contracts.Assistant;

/// <summary>"Potwierdź" or "Anuluj" on a card in a channel (JV9); JARVIS checks it is the person who asked, in time.</summary>
public sealed record AssistantConfirmed(Guid ConfirmationId, Guid UserId, bool Approved, string Channel, string Reference) : IIntegrationEvent;

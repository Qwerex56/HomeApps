namespace HomeApps.Contracts.Assistant;

/// <summary>A line of the action card (jarvis-v1.md 5.4).</summary>
/// <param name="Status"><c>Done</c>, <c>Failed</c>, <c>NeedsConfirmation</c>, <c>Denied</c> or <c>Expired</c>.</param>
/// <param name="ConfirmationId">With <c>NeedsConfirmation</c>: what <see cref="AssistantConfirmed"/> names, until <paramref name="ConfirmBy"/>.</param>
public sealed record AssistantAction(string Description, string Status, string? Url, Guid? ConfirmationId, DateTimeOffset? ConfirmBy);

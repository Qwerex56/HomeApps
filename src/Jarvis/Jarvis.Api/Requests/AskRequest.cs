namespace Jarvis.Api.Requests;

/// <param name="HouseholdId">The household chosen in the app (Q6), the default for the tools.</param>
internal sealed record AskRequest(string Text, Guid? HouseholdId);

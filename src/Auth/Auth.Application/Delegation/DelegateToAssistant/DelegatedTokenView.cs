namespace Auth.Application.Delegation.DelegateToAssistant;

public sealed record DelegatedTokenView(string AccessToken, DateTimeOffset ExpiresAt);

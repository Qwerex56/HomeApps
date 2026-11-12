namespace Jarvis.Infrastructure.Gateway;

/// <summary>Auth's answer to <c>POST /internal/delegation</c>.</summary>
internal sealed record DelegatedToken(string AccessToken, DateTimeOffset ExpiresAt);

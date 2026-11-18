namespace Jarvis.VoiceBridge;

/// <summary>Auth's <c>POST /auth/devices/token</c>: a 15-minute token of the household satellite.</summary>
internal sealed record DeviceToken(string AccessToken, DateTimeOffset ExpiresAt);

namespace Jarvis.Infrastructure.Brain;

/// <summary>What the admin sees of the connection: never the key, only whether one is set.</summary>
public sealed record LlmSettingsView(string BaseUrl, string Model, bool ApiKeySet, int TimeoutSeconds);

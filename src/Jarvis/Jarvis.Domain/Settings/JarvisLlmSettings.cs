namespace Jarvis.Domain.Settings;

/// <summary>
/// The model connection an admin sets in the web app (jarvis-desktop-rework D14): one row. The API key is stored only
/// protected (ASP.NET Data Protection) and never leaves the server.
/// </summary>
public sealed class JarvisLlmSettings
{
    public const int SingleId = 1;

    public int Id { get; private set; } = SingleId;

    public string BaseUrl { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string? ApiKeyProtected { get; private set; }

    public int TimeoutSeconds { get; private set; }

    public void Set(string baseUrl, string model, int timeoutSeconds)
    {
        BaseUrl = baseUrl;
        Model = model;
        TimeoutSeconds = timeoutSeconds;
    }

    public void SetApiKey(string? protectedKey) => ApiKeyProtected = protectedKey;
}

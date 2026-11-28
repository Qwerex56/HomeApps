namespace Jarvis.Api.Requests;

/// <param name="ApiKey">Absent keeps the stored key, an empty string removes it.</param>
internal sealed record SaveLlmSettingsRequest(string? BaseUrl, string? Model, string? ApiKey, int TimeoutSeconds);

using System.Text.Json;

namespace Jarvis.Api.Tests;

/// <summary>What a fake source sees of the token it got: the person and whether an assistant acts for them.</summary>
internal static class Who
{
    public static string Of(string? authorization)
    {
        var payload = authorization!["Bearer ".Length..].Split('.')[1];
        var padded = payload.Replace('-', '+').Replace('_', '/').PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(padded));
        var root = json.RootElement;
        var act = root.TryGetProperty("act", out var actor) ? actor.GetProperty("sub").GetString() : "none";
        return $"sub={root.GetProperty("sub").GetString()} act={act}";
    }
}

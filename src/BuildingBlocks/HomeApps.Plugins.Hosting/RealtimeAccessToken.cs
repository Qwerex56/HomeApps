using HomeApps.Plugins.Manifest;
using Microsoft.AspNetCore.Http;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// The access token a browser sends to the plugin's realtime hub as <c>?access_token=</c> (WebSockets and server-sent
/// events cannot carry an Authorization header). Read only on the hub's own path, never elsewhere, so a token in a
/// link to the API is ignored.
/// </summary>
internal static class RealtimeAccessToken
{
    public const string QueryName = "access_token";

    public static string? Read(HttpRequest request, PluginManifest manifest)
    {
        if (manifest.Realtime is null || !request.Path.StartsWithSegments($"/{manifest.Realtime}"))
        {
            return null;
        }

        var token = request.Query[QueryName].ToString();
        return string.IsNullOrEmpty(token) ? null : token;
    }
}

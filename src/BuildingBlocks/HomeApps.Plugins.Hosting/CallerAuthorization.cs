using HomeApps.Plugins.Manifest;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HomeApps.Plugins.Hosting;

/// <summary>The caller's own <c>Authorization</c> header, or the realtime hub's <c>?access_token=</c>, to pass on to Households.</summary>
internal static class CallerAuthorization
{
    public static string? Read(IHttpContextAccessor accessor, PluginManifest manifest)
    {
        if (accessor.HttpContext?.Request is not { } request)
        {
            return null;
        }

        var header = request.Headers[HeaderNames.Authorization].ToString();
        if (!string.IsNullOrEmpty(header))
        {
            return header;
        }

        return RealtimeAccessToken.Read(request, manifest) is { } token ? $"Bearer {token}" : null;
    }
}

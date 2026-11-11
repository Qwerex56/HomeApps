using System.Text;

namespace Auth.Application.ConnectedApps;

/// <summary>The app's redirect URI with OAuth's answer added to its query (OAuth 2.1 4.1.2).</summary>
internal static class RedirectLocation
{
    public static string With(string redirectUri, params (string Name, string? Value)[] parameters)
    {
        var location = new StringBuilder(redirectUri);
        var separator = redirectUri.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        foreach (var (name, value) in parameters.Where(p => p.Value is not null))
        {
            location.Append(separator).Append(name).Append('=').Append(Uri.EscapeDataString(value!));
            separator = '&';
        }

        return location.ToString();
    }
}

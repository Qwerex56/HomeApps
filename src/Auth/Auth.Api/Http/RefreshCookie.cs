namespace Auth.Api.Http;

/// <summary>Browser clients keep the refresh token here, out of reach of scripts (auth.md 3).</summary>
internal static class RefreshCookie
{
    public const string Name = "ha_refresh";

    // __Host- would need Path=/; the cookie only ever goes to /auth.
    private const string Path = "/auth";

    public static void Set(HttpContext http, string token, DateTimeOffset expiresAt) =>
        http.Response.Cookies.Append(Name, token, Options(expiresAt));

    public static string? Read(HttpContext http) =>
        http.Request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    public static void Delete(HttpContext http) => http.Response.Cookies.Delete(Name, Options(null));

    private static CookieOptions Options(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true,
    };
}

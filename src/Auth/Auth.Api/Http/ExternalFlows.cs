using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Infrastructure.External;
using Auth.Infrastructure.Security;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Api.Http;

/// <summary>
/// One sign-in (or A-11 link) through Google, between the redirect out and the callback. It lives only in the
/// encrypted <c>ha_ext</c> cookie (Q13): nothing in the database, and the browser cannot read or change it.
/// </summary>
internal sealed record ExternalFlow(
    string State,
    string CodeVerifier,
    ExternalProvider Provider,
    string ClientId,
    Uri ReturnUrl,
    string? Invitation,
    string? Locale,
    Guid? LinkIdentityId,
    DateTimeOffset ExpiresAt);

/// <summary>Authorization code + PKCE (A-05, A-11). The provider only ever sees the S256 challenge.</summary>
internal sealed class ExternalFlows(
    IDataProtectionProvider dataProtection,
    IClientRegistry clients,
    IOptions<GoogleOptions> google,
    IConfigurationManager<OpenIdConnectConfiguration> googleMetadata,
    IOptions<SigningOptions> signing,
    TimeProvider time)
{
    public const string CookieName = "ha_ext";
    private const string CookiePath = "/auth/external";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly IDataProtector _protector = dataProtection.CreateProtector("Auth.ExternalFlow.v1");

    public static Error BrowserOnly => new AppError("external_login.browser_only",
        "Signing in with Google is only available in the browser app.", ErrorKind.Validation);

    public static Error ReturnUrlNotAllowed => new AppError("client.return_url_not_allowed",
        "The return address is not registered for this client.", ErrorKind.Validation);

    /// <summary>Sets the flow cookie and returns where to send the browser.</summary>
    public async Task<Result<Uri>> StartAsync(
        HttpContext http,
        ExternalProvider provider,
        string? clientId,
        string? returnUrl,
        string? invitation,
        string? locale,
        Guid? linkIdentityId,
        CancellationToken cancellationToken)
    {
        if (provider != ExternalProvider.Google || !google.Value.IsConfigured)
        {
            return Result.Fail<Uri>(ApplicationErrors.ExternalProviderDisabled);
        }

        var client = clients.Find(clientId ?? string.Empty);
        if (client is null)
        {
            return Result.Fail<Uri>(CommonErrors.ClientInvalid);
        }

        // The tokens come back in a cookie, so only browser clients (Native signs in with a password in 1.0).
        if (client.Type != ClientType.Browser)
        {
            return Result.Fail<Uri>(BrowserOnly);
        }

        var target = ResolveReturnUrl(client, returnUrl);
        if (target is null)
        {
            return Result.Fail<Uri>(ReturnUrlNotAllowed);
        }

        var flow = new ExternalFlow(
            RandomValue(16), RandomValue(32), provider, client.ClientId, target, invitation, locale, linkIdentityId,
            time.GetUtcNow() + Lifetime);
        http.Response.Cookies.Append(CookieName, _protector.Protect(JsonSerializer.Serialize(flow)), CookieOptions(flow.ExpiresAt));

        var metadata = await googleMetadata.GetConfigurationAsync(cancellationToken);
        var url = QueryHelpers.AddQueryString(metadata.AuthorizationEndpoint, new Dictionary<string, string?>
        {
            ["client_id"] = google.Value.ClientId,
            ["redirect_uri"] = CallbackUri(provider).ToString(),
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = flow.State,
            ["code_challenge"] = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(flow.CodeVerifier))),
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account",
        });
        return new Uri(url);
    }

    /// <summary>The flow the callback belongs to, or null when the cookie is missing, expired, forged or for another state.</summary>
    public ExternalFlow? Complete(HttpContext http, string? state)
    {
        if (!http.Request.Cookies.TryGetValue(CookieName, out var cookie) || string.IsNullOrEmpty(cookie))
        {
            return null;
        }

        http.Response.Cookies.Delete(CookieName, CookieOptions(null));

        ExternalFlow? flow;
        try
        {
            flow = JsonSerializer.Deserialize<ExternalFlow>(_protector.Unprotect(cookie));
        }
        catch (Exception e) when (e is CryptographicException or JsonException)
        {
            return null;
        }

        var sameState = flow is not null && state is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(flow.State), Encoding.ASCII.GetBytes(state));
        return sameState && flow!.ExpiresAt > time.GetUtcNow() ? flow : null;
    }

    /// <summary>Registered at Google as the redirect URI; built from the issuer, Auth's public address.</summary>
    public Uri CallbackUri(ExternalProvider provider) =>
        new($"{signing.Value.Issuer.TrimEnd('/')}/auth/external/{ProviderSegment(provider)}/callback");

    /// <summary>
    /// Only addresses under one of the client's <see cref="RegisteredClient.RedirectUris"/> (same origin, path below it).
    /// A relative one is resolved against the first; none means the first itself.
    /// </summary>
    internal static Uri? ResolveReturnUrl(RegisteredClient client, string? returnUrl)
    {
        if (client.RedirectUris.Count == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return client.RedirectUris[0];
        }

        if (!Uri.TryCreate(client.RedirectUris[0], returnUrl, out var candidate))
        {
            return null;
        }

        candidate = new UriBuilder(candidate) { Fragment = string.Empty }.Uri;
        return client.RedirectUris.Any(allowed =>
            Uri.Compare(allowed, candidate, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0
            && candidate.AbsolutePath.StartsWith(allowed.AbsolutePath, StringComparison.Ordinal))
            ? candidate
            : null;
    }

    /// <summary>The provider as it appears in routes: <c>google</c>.</summary>
    public static string ProviderSegment(ExternalProvider provider) => provider switch
    {
        ExternalProvider.Google => "google",
        ExternalProvider.Apple => "apple",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
    };

    public static bool TryParseProvider(string segment, out ExternalProvider provider)
    {
        foreach (var candidate in Enum.GetValues<ExternalProvider>())
        {
            if (string.Equals(ProviderSegment(candidate), segment, StringComparison.OrdinalIgnoreCase))
            {
                provider = candidate;
                return true;
            }
        }

        provider = default;
        return false;
    }

    private static string RandomValue(int bytes) => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(bytes));

    // Lax, not Strict: the callback is a navigation coming back from Google's domain.
    private static CookieOptions CookieOptions(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = CookiePath,
        Expires = expiresAt,
        IsEssential = true,
    };
}

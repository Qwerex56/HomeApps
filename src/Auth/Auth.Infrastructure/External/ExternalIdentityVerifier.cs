using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Auth.Application.Abstractions;
using Auth.Application.Common;
using Auth.Domain.Identities;
using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.External;

/// <summary>
/// Google (A-05, A-06, A-11): exchanges the authorization code with its PKCE verifier for tokens, then validates the
/// ID token against Google's published keys. Apple is not in 1.0 (Q10).
/// </summary>
internal sealed partial class ExternalIdentityVerifier(
    HttpClient http,
    IConfigurationManager<OpenIdConnectConfiguration> googleMetadata,
    IOptions<GoogleOptions> options,
    TimeProvider time,
    ILogger<ExternalIdentityVerifier> logger) : IExternalIdentityVerifier
{
    private static readonly string[] GoogleIssuers = ["https://accounts.google.com", "accounts.google.com"];
    private static readonly JsonWebTokenHandler Handler = new();

    private static Error ProviderDisabled => ApplicationErrors.ExternalProviderDisabled;

    private static Error Failed => ApplicationErrors.ExternalLoginFailed;

    public async Task<Result<ExternalIdentity>> VerifyAsync(
        ExternalProvider provider, string code, string codeVerifier, Uri redirectUri, CancellationToken cancellationToken)
    {
        var google = options.Value;
        if (provider != ExternalProvider.Google || !google.IsConfigured)
        {
            return Result.Fail<ExternalIdentity>(ProviderDisabled);
        }

        using var response = await http.PostAsync(google.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = redirectUri.ToString(),
            ["client_id"] = google.ClientId!,
            ["client_secret"] = google.ClientSecret!,
        }), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            LogExchangeFailed(logger, (int)response.StatusCode);
            return Result.Fail<ExternalIdentity>(Failed);
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (string.IsNullOrEmpty(tokens?.IdToken))
        {
            return Result.Fail<ExternalIdentity>(Failed);
        }

        var metadata = await googleMetadata.GetConfigurationAsync(cancellationToken);
        var validation = await Handler.ValidateTokenAsync(tokens.IdToken, new TokenValidationParameters
        {
            ValidIssuers = GoogleIssuers,
            ValidAudience = google.ClientId,
            IssuerSigningKeys = metadata.SigningKeys,
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = time.GetUtcNow().UtcDateTime.AddMinutes(1);
                return (notBefore is null || notBefore <= now) && expires > now.AddMinutes(-2);
            },
        });
        if (!validation.IsValid || validation.SecurityToken is not JsonWebToken idToken)
        {
            LogInvalidIdToken(logger, validation.Exception?.GetType().Name);
            return Result.Fail<ExternalIdentity>(Failed);
        }

        return Read(idToken);
    }

    /// <summary>The claims Google puts in its ID token (<c>email_verified</c> may come as a bool or a string).</summary>
    internal static Result<ExternalIdentity> Read(JsonWebToken idToken)
    {
        string? Text(string name) => idToken.TryGetPayloadValue<string>(name, out var value) ? value : null;
        var verified = idToken.TryGetPayloadValue<bool>("email_verified", out var flag)
            ? flag
            : string.Equals(Text("email_verified"), "true", StringComparison.OrdinalIgnoreCase);

        var subject = Text(JwtRegisteredClaimNames.Sub);
        var email = Text(JwtRegisteredClaimNames.Email);
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email))
        {
            return Result.Fail<ExternalIdentity>(Failed);
        }

        var picture = Uri.TryCreate(Text("picture"), UriKind.Absolute, out var uri) ? uri : null;
        return new ExternalIdentity(subject, email, verified, Text("given_name"), Text("family_name"), picture);
    }

    private sealed record TokenResponse([property: JsonPropertyName("id_token")] string? IdToken);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google refused the authorization code exchange with status {Status}")]
    private static partial void LogExchangeFailed(ILogger logger, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google ID token failed validation: {Reason}")]
    private static partial void LogInvalidIdToken(ILogger logger, string? reason);
}

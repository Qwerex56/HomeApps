using FluentResults;
using HomeApps.BuildingBlocks.Api.Auth;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using Jarvis.Api.Requests;
using Jarvis.Infrastructure.Brain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Jarvis.Api.Endpoints;

/// <summary>
/// The model connection from the admin's account (jarvis-desktop-rework D14): admin or owner only, by their own token (an
/// assistant's delegated one is refused). The key goes in and never comes out, <c>apiKeySet</c> says whether there is one.
/// </summary>
internal sealed class LlmSettingsEndpoints : IEndpoint
{
    private static readonly AppError Forbidden = new("jarvis.forbidden", "Only an administrator can change the model connection.", ErrorKind.Forbidden);
    private static readonly AppError InvalidUrl = new("jarvis.llm_url", "The model URL must be an http or https address.", ErrorKind.Validation);
    private static readonly AppError InvalidModel = new("jarvis.llm_model", "The model name is required.", ErrorKind.Validation);
    private static readonly AppError InvalidTimeout = new(
        "jarvis.llm_timeout", $"The timeout must be between 1 and {LlmSettingsStore.MaxTimeoutSeconds} seconds.", ErrorKind.Validation);

    private static readonly AppError TooLong = new(
        "jarvis.llm_length", "The URL, the model name or the key is too long.", ErrorKind.Validation);
    private static readonly AppError KeyRequired = new(
        "jarvis.llm_key_required", "The address points to another host: enter the API key again, the stored one is not sent there.", ErrorKind.Validation);

    private const int MaxUrlLength = 500;
    private const int MaxModelLength = 200;
    private const int MaxKeyLength = 1000;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var settings = app.MapVersionedGroup("/jarvis/api").MapGroup("/admin/llm-settings").WithTags("JARVIS");

        settings.MapGet("/", async (HttpContext http, ICurrentUser user, IOptions<JwtOptions> jwt, LlmSettingsStore store, CancellationToken ct) =>
            IsAdmin(http, user, jwt.Value) ? HttpResults.Ok(await store.GetAsync(ct)) : Result.Fail(Forbidden).ToHttpResult())
            .Produces<LlmSettingsView>();

        settings.MapPut("/", async (SaveLlmSettingsRequest body, HttpContext http, ICurrentUser user, IOptions<JwtOptions> jwt, LlmSettingsStore store, OpenAiModelHealthCheck check, CancellationToken ct) =>
        {
            if (!IsAdmin(http, user, jwt.Value))
            {
                return Result.Fail(Forbidden).ToHttpResult();
            }

            if (!Uri.TryCreate(body.BaseUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length > 0)
            {
                return Result.Fail(InvalidUrl).ToHttpResult();
            }

            if (string.IsNullOrWhiteSpace(body.Model))
            {
                return Result.Fail(InvalidModel).ToHttpResult();
            }

            if (body.BaseUrl!.Trim().Length > MaxUrlLength || body.Model.Trim().Length > MaxModelLength || body.ApiKey?.Trim().Length > MaxKeyLength)
            {
                return Result.Fail(TooLong).ToHttpResult();
            }

            if (body.TimeoutSeconds is < 1 or > LlmSettingsStore.MaxTimeoutSeconds)
            {
                return Result.Fail(InvalidTimeout).ToHttpResult();
            }

            // The stored key must not travel to another host: a new origin needs the key typed again.
            var current = await store.CurrentAsync(ct);
            if (body.ApiKey is null && !string.IsNullOrWhiteSpace(current.ApiKey) && !SameOrigin(current.BaseUrl, uri))
            {
                return Result.Fail(KeyRequired).ToHttpResult();
            }

            await store.SaveAsync(body.BaseUrl!, body.Model, body.ApiKey, body.TimeoutSeconds, ct);

            // A save checks the model again, so a wrong URL or key shows in the logs at once.
            await check.CheckAsync(await store.CurrentAsync(ct), ct);
            return HttpResults.Ok(await store.GetAsync(ct));
        })
        .Produces<LlmSettingsView>();

        settings.MapPost("/test", async (HttpContext http, ICurrentUser user, IOptions<JwtOptions> jwt, LlmSettingsStore store, OpenAiModelHealthCheck check, CancellationToken ct) =>
            IsAdmin(http, user, jwt.Value) ? HttpResults.Ok(await check.CheckAsync(await store.CurrentAsync(ct), ct)) : Result.Fail(Forbidden).ToHttpResult())
            .Produces<LlmCheckResult>();
    }

    private static bool SameOrigin(string? stored, Uri next) =>
        Uri.TryCreate(stored?.Trim(), UriKind.Absolute, out var old)
        && string.Equals(old.GetLeftPart(UriPartial.Authority), next.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);

    private static bool IsAdmin(HttpContext http, ICurrentUser user, JwtOptions jwt) =>
        !user.IsDelegated
        && http.User.Claims.Any(c => (c.Type == ClaimNames.Audience || c.Type == "aud") && string.Equals(c.Value, jwt.Audience, StringComparison.Ordinal))
        && user.Roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "Owner", StringComparison.OrdinalIgnoreCase));
}

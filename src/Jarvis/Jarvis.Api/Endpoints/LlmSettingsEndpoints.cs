using FluentResults;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using Jarvis.Api.Requests;
using Jarvis.Infrastructure.Brain;
using Microsoft.AspNetCore.Http;
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

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var settings = app.MapVersionedGroup("/jarvis/api").MapGroup("/admin/llm-settings").WithTags("JARVIS");

        settings.MapGet("/", async (ICurrentUser user, LlmSettingsStore store, CancellationToken ct) =>
            IsAdmin(user) ? HttpResults.Ok(await store.GetAsync(ct)) : Result.Fail(Forbidden).ToHttpResult())
            .Produces<LlmSettingsView>();

        settings.MapPut("/", async (SaveLlmSettingsRequest body, ICurrentUser user, LlmSettingsStore store, OpenAiModelHealthCheck check, CancellationToken ct) =>
        {
            if (!IsAdmin(user))
            {
                return Result.Fail(Forbidden).ToHttpResult();
            }

            if (!Uri.TryCreate(body.BaseUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            {
                return Result.Fail(InvalidUrl).ToHttpResult();
            }

            if (string.IsNullOrWhiteSpace(body.Model))
            {
                return Result.Fail(InvalidModel).ToHttpResult();
            }

            if (body.TimeoutSeconds is < 1 or > LlmSettingsStore.MaxTimeoutSeconds)
            {
                return Result.Fail(InvalidTimeout).ToHttpResult();
            }

            await store.SaveAsync(body.BaseUrl!, body.Model, body.ApiKey, body.TimeoutSeconds, ct);

            // A save checks the model again, so a wrong URL or key shows in the logs at once.
            await check.CheckAsync(await store.CurrentAsync(ct), ct);
            return HttpResults.Ok(await store.GetAsync(ct));
        })
        .Produces<LlmSettingsView>();

        settings.MapPost("/test", async (ICurrentUser user, LlmSettingsStore store, OpenAiModelHealthCheck check, CancellationToken ct) =>
            IsAdmin(user) ? HttpResults.Ok(await check.CheckAsync(await store.CurrentAsync(ct), ct)) : Result.Fail(Forbidden).ToHttpResult())
            .Produces<LlmCheckResult>();
    }

    private static bool IsAdmin(ICurrentUser user) =>
        !user.IsDelegated && user.Roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "Owner", StringComparison.OrdinalIgnoreCase));
}

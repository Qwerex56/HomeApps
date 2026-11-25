using Accounts.Api.Http;
using Accounts.Application.Common;
using Accounts.Application.MyAccount;
using Accounts.Domain.Users;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Accounts.Api.Endpoints;

internal sealed record DisableRequest(string? Reason);

/// <summary>UC-01 to UC-04: the signed-in user's own account. Email and password live in Auth (<c>/auth/me</c>).</summary>
internal sealed class MeEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var me = app.MapVersionedGroup("/users").MapGroup("/me").WithTags("My account");

        me.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyAccountQuery(), ct)).ToHttpResult());

        me.MapPut("/profile", async (ProfileInput body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateMyProfileCommand(body), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        me.MapPost("/avatar", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            if (file.Length > AvatarImage.MaxBytes)
            {
                return ResultHttpMapper.ToProblem([UserErrors.AvatarTooLarge]);
            }

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return (await sender.Send(new UploadMyAvatarCommand(buffer.ToArray()), ct)).ToHttpResult();
        })
            .Produces<AvatarUploadedView>()
            .DisableAntiforgery()
            .RequireRateLimiting(RateLimits.Changes);

        me.MapPost("/disable", async (DisableRequest? body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DisableMyAccountCommand(body?.Reason), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        me.MapGet("/history", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyHistoryQuery(), ct)).ToHttpResult());

        // 2.15: served by user id (not /me), so another user or a plugin can show the picture. Open: it is only an avatar.
        app.MapVersionedGroup("/users").MapGet("/{userId:guid}/avatar", async (Guid userId, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserAvatarQuery(userId), ct);
            if (result.IsFailed)
            {
                return ResultHttpMapper.ToProblem(result.Errors);
            }

            var avatar = result.Value;
            http.Response.Headers.CacheControl = "public, max-age=86400, stale-while-revalidate=3600";
            return http.Request.Headers.IfNoneMatch.Contains(avatar.ETag)
                ? Results.StatusCode(StatusCodes.Status304NotModified)
                : Results.File(avatar.Content, avatar.ContentType, entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue(avatar.ETag));
        })
            .WithTags("My account")
            .AllowAnonymous();
    }
}

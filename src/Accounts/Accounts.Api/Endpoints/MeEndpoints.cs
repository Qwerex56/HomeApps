using Accounts.Api.Http;
using Accounts.Application.Common;
using Accounts.Application.MyAccount;
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
        var me = app.MapGroup("/users/me").WithTags("My account");

        me.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyAccountQuery(), ct)).ToHttpResult());

        me.MapPut("/profile", async (ProfileInput body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateMyProfileCommand(body), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        me.MapPost("/disable", async (DisableRequest? body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DisableMyAccountCommand(body?.Reason), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        me.MapGet("/history", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyHistoryQuery(), ct)).ToHttpResult());
    }
}

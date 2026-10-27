using Accounts.Api.Http;
using Accounts.Application.Common;
using Accounts.Application.Reactivation;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Accounts.Api.Endpoints;

/// <summary>
/// UC-05: only with the reactivation ticket from Auth. The form shows the current profile; the confirmed profile
/// brings the account back, then the app finishes signing in at Auth with the same ticket.
/// </summary>
internal sealed class ReactivationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var reactivation = app.MapGroup("/users/me").WithTags("My account").RequireAuthorization(ReactivationTicket.Policy);

        reactivation.MapGet("/reactivation", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetReactivationFormQuery(), ct)).ToHttpResult());

        reactivation.MapPost("/reactivate", async (ProfileInput body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ReactivateMyAccountCommand(body), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);
    }
}

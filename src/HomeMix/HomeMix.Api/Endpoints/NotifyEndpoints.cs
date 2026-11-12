using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Http;
using HomeMix.Api.Requests;
using HomeMix.Application.Notify.PushNotification;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary><c>notify.push</c>: the web app's bridge sends an add-on's notification on the person's behalf, within a limit per installation.</summary>
internal sealed class NotifyEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/homemix/api/installations/{id:guid}/notify", async (Guid id, NotifyRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new PushNotificationCommand(id, body.UserIds, body.Title, body.Body, body.Path, body.Tag), ct)).ToHttpResult())
            .WithTags("Notifications")
            .RequireRateLimiting(NotifyLimit.Policy)
            .Produces<PushNotificationResult>();
}

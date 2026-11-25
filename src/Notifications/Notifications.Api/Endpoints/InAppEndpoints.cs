using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using Notifications.Application.InApp;
using Notifications.Application.InApp.ListInApp;
using Notifications.Application.InApp.MarkAllInAppRead;
using Notifications.Application.InApp.MarkInAppRead;

namespace Notifications.Api.Endpoints;

/// <summary>2.13: my notification center. Always the caller from the token.</summary>
internal sealed class InAppEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var inApp = app.MapVersionedGroup("/notifications").MapGroup("/in-app").WithTags("Notification center");

        inApp.MapGet("/", async (ISender sender, CancellationToken ct, int limit = 50, bool unreadOnly = false) =>
            (await sender.Send(new ListInAppQuery(limit, unreadOnly), ct)).ToHttpResult())
            .Produces<NotificationListDto>();

        inApp.MapPost("/read-all", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new MarkAllInAppReadCommand(), ct)).ToHttpResult());

        inApp.MapPost("/{id:guid}/read", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new MarkInAppReadCommand(id), ct)).ToHttpResult());
    }
}

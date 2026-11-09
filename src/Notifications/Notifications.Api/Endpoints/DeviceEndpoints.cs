using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Notifications.Api.Requests;
using Notifications.Application.Devices.ForgetDevice;
using Notifications.Application.Devices.RegisterDevice;
using Notifications.Application.Devices.RemoveDevice;
using Notifications.Infrastructure.Push;

namespace Notifications.Api.Endpoints;

/// <summary>Turning notifications on and off on a device, and the VAPID key a browser subscribes with.</summary>
internal sealed class DeviceEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var notifications = app.MapGroup("/notifications").WithTags("Devices");

        notifications.MapGet("/vapid-public-key", (VapidKeys keys) => TypedResults.Ok(new VapidPublicKey(keys.PublicKey)));

        var devices = notifications.MapGroup("/devices");

        devices.MapPut("/", async (RegisterDeviceRequest body, HttpRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new RegisterDeviceCommand(body.Endpoint, body.Keys?.P256dh ?? string.Empty, body.Keys?.Auth ?? string.Empty,
                    request.Headers[HeaderNames.UserAgent].ToString()),
                ct)).Map(id => new DeviceRegistered(id)).ToHttpResult())
            .Produces<DeviceRegistered>();

        devices.MapDelete("/", async ([FromQuery] string endpoint, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ForgetDeviceCommand(endpoint), ct)).ToHttpResult());

        devices.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RemoveDeviceCommand(id), ct)).ToHttpResult());
    }
}

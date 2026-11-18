using Devices.Api.Requests;
using Devices.Application.Common;
using Devices.Application.MyDevices.AllowInAdvance;
using Devices.Application.MyDevices.GetMyDevices;
using Devices.Application.MyDevices.RemoveDevice;
using Devices.Application.MyDevices.RestoreDevice;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Devices.Api.Endpoints;

/// <summary>My devices (jarvis-satellites.md 2a): the list, removing and restoring, and "Nie pytaj" per action (S6).</summary>
internal sealed class MyDevicesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var devices = app.MapGroup("/devices").WithTags("Devices");

        devices.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyDevicesQuery(), ct)).ToHttpResult())
            .Produces<IReadOnlyList<DeviceView>>();

        devices.MapDelete("/{deviceId:guid}", async (Guid deviceId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RemoveDeviceCommand(deviceId), ct)).ToHttpResult());

        devices.MapPost("/{deviceId:guid}/restore", async (Guid deviceId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RestoreDeviceCommand(deviceId), ct)).ToHttpResult());

        devices.MapPut("/{deviceId:guid}/actions/{action}/allow", async (
                Guid deviceId, string action, AllowInAdvanceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AllowInAdvanceCommand(deviceId, action, body.Allowed), ct)).ToHttpResult());
    }
}

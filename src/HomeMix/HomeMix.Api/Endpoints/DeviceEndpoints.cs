using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Requests;
using HomeMix.Application.Devices;
using HomeMix.Application.Devices.ChooseWidgetDevice;
using HomeMix.Application.Devices.GetInstallationDeviceState;
using HomeMix.Application.Devices.ListInstallationDevices;
using HomeMix.Application.Devices.SetInstallationDevice;
using HomeMix.Application.Installations;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// What the web app's bridge calls for an add-on's <c>devices</c> and <c>devices.control</c> capabilities (docs/homemix-authoring.md,
/// "Devices"): list or match the household's devices, read a state, set a control, and the admins' manual pick for a
/// widget. Everything goes to Devices with the person's own token, after the installation's consent is checked.
/// </summary>
internal sealed class DeviceEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/homemix/api/installations/{id:guid}").WithTags("Devices");

        api.MapGet("/devices", async (
                Guid id, ISender sender, CancellationToken ct,
                string? widget = null, string? capability = null, string? role = null, string? model = null, bool state = false) =>
            (await sender.Send(new ListInstallationDevicesQuery(id, widget, capability, role, model, state), ct)).ToHttpResult())
            .Produces<InstallationDevicesView>();

        api.MapGet("/devices/{deviceId:guid}/state", async (Guid id, Guid deviceId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetInstallationDeviceStateQuery(id, deviceId), ct)).ToHttpResult());

        api.MapPost("/devices/{deviceId:guid}/set", async (Guid id, Guid deviceId, SetDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetInstallationDeviceCommand(id, deviceId, body.Control ?? string.Empty, body.Value), ct)).ToHttpResult());

        api.MapPut("/widget-devices/{widgetId}", async (Guid id, string widgetId, ChooseWidgetDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ChooseWidgetDeviceCommand(id, widgetId, body.Device), ct)).ToHttpResult())
            .Produces<InstallationView>();

        api.MapDelete("/widget-devices/{widgetId}", async (Guid id, string widgetId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ChooseWidgetDeviceCommand(id, widgetId, null), ct)).ToHttpResult())
            .Produces<InstallationView>();
    }
}

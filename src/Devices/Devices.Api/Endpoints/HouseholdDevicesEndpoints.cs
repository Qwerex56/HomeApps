using Devices.Api.Requests;
using Devices.Application.Common;
using Devices.Application.HouseholdDevices;
using Devices.Application.HouseholdDevices.AddHouseholdDevice;
using Devices.Application.HouseholdDevices.GetHouseholdDevices;
using Devices.Application.HouseholdDevices.NewPairingCode;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Devices.Api.Endpoints;

/// <summary>
/// A household's satellites (jarvis-satellites.md 3a, S-1a): the list for members, adding one and a new pairing code
/// for admins. Under <c>/devices</c>, since Traefik sends <c>/households</c> to Households.
/// </summary>
internal sealed class HouseholdDevicesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var household = app.MapGroup("/devices/households/{householdId:guid}").WithTags("Devices");

        household.MapGet("/", async (Guid householdId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetHouseholdDevicesQuery(householdId), ct)).ToHttpResult())
            .Produces<IReadOnlyList<DeviceView>>();

        household.MapPost("/", async (Guid householdId, AddHouseholdDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AddHouseholdDeviceCommand(householdId, body.Name), ct)).ToHttpResult())
            .Produces<PairingCodeView>();

        app.MapPost("/devices/{deviceId:guid}/pairing-code", async (Guid deviceId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new NewPairingCodeCommand(deviceId), ct)).ToHttpResult())
            .WithTags("Devices")
            .Produces<PairingCodeView>();
    }
}

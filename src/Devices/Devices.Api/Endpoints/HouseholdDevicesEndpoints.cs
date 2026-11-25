using Devices.Api.Requests;
using Devices.Application.Common;
using Devices.Application.HouseholdDevices;
using Devices.Application.HouseholdDevices.AddHouseholdDevice;
using Devices.Application.HouseholdDevices.GetHouseholdDevices;
using Devices.Application.HouseholdDevices.NewPairingCode;
using Devices.Application.HouseholdDevices.PairDevice;
using Devices.Application.HouseholdDevices.RenameDevice;
using Devices.Application.HouseholdDevices.SetAdminNotes;
using Devices.Application.HouseholdDevices.SetJarvisAccess;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Devices.Api.Endpoints;

/// <summary>
/// A household's satellites (jarvis-satellites.md 3a, S-1a): the list for members, adding one and a new pairing code
/// for admins, renaming and notes for JARVIS (admins), and pairing for the device itself (S-1b), with its descriptor. Under <c>/devices</c>, since Traefik sends <c>/households</c> to Households.
/// </summary>
internal sealed class HouseholdDevicesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var devices = app.MapVersionedGroup("/devices");

        var household = app.MapVersionedGroup("/devices").MapGroup("/households/{householdId:guid}").WithTags("Devices");

        // ?capability=&role=&model= narrow the list to matching devices (2.5), ?state=true adds each one's last state.
        household.MapGet("/", async (
                Guid householdId, ISender sender, CancellationToken ct, string? capability = null, string? role = null, string? model = null, bool state = false) =>
            (await sender.Send(new GetHouseholdDevicesQuery(householdId, capability, role, model, state), ct)).ToHttpResult())
            .Produces<IReadOnlyList<DeviceView>>();

        household.MapPost("/", async (Guid householdId, AddHouseholdDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AddHouseholdDeviceCommand(householdId, body.Name), ct)).ToHttpResult())
            .Produces<PairingCodeView>();

        devices.MapPost("/{deviceId:guid}/pairing-code", async (Guid deviceId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new NewPairingCodeCommand(deviceId), ct)).ToHttpResult())
            .WithTags("Devices")
            .Produces<PairingCodeView>();

        // D6: admins (and a person for their own computer) rename a device and write notes for JARVIS.
        devices.MapPut("/{deviceId:guid}/name", async (Guid deviceId, RenameDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RenameDeviceCommand(deviceId, body.Name), ct)).ToHttpResult())
            .WithTags("Devices");

        devices.MapPut("/{deviceId:guid}/notes", async (Guid deviceId, SetAdminNotesRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetAdminNotesCommand(deviceId, body.Notes), ct)).ToHttpResult())
            .WithTags("Devices");

        // D6: admins decide whether JARVIS may control the device and whether it asks first ("Nie pytaj").
        devices.MapPut("/{deviceId:guid}/jarvis", async (Guid deviceId, SetJarvisAccessRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetJarvisAccessCommand(deviceId, body.MayControl, body.Asks), ct)).ToHttpResult())
            .WithTags("Devices");

        // S13: the satellite has no account yet, only the code (8 of 31 characters, valid 10 minutes, one use).
        devices.MapPost("/pair", async (PairDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new PairDeviceCommand(body.Code, body.Descriptor is { ValueKind: not System.Text.Json.JsonValueKind.Null } d ? d.GetRawText() : null), ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(DependencyInjection.PairRateLimit)
            .WithMetadata(new RequestSizeLimitAttribute(256 * 1024))
            .WithTags("Devices")
            .Produces<PairedDeviceView>();
    }
}

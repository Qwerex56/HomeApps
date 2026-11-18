using Auth.Api.Http;
using Auth.Application.Devices;
using Auth.Application.Devices.IdentifySpeaker;
using Auth.Application.Devices.IssueDeviceToken;
using Auth.Application.Devices.PairDevice;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Auth.Api.Endpoints;

/// <summary>
/// Household satellites (jarvis-satellites.md 3a, S13). Devices pairs one on the internal network with its service
/// secret; the satellite then trades its own secret for short access tokens, like OAuth's client credentials.
/// </summary>
internal sealed class DeviceEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/devices", async (PairDeviceRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new PairDeviceCommand(body.ClientId, body.ClientSecret, body.DeviceId, body.HouseholdId, body.Name), ct))
            .ToHttpResult())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<PairedDeviceView>();

        // S4: JARVIS asks whose PIN a satellite heard (internal network, his service secret).
        app.MapPost("/internal/devices/speaker", async (IdentifySpeakerRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new IdentifySpeakerCommand(body.ClientId, body.ClientSecret, body.DeviceId, body.Candidates, body.Pin), ct))
            .ToHttpResult())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<SpeakerView>();

        app.MapPost("/auth/devices/token", async (DeviceTokenRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new IssueDeviceTokenCommand(body.DeviceId, body.Secret), ct)).ToHttpResult())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimits.Refresh)
            .WithTags("Devices")
            .Produces<DeviceTokenView>();
    }
}

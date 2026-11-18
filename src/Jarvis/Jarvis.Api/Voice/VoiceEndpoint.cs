using HomeApps.BuildingBlocks.Api.Auth;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Voice;
using Microsoft.Extensions.Options;

namespace Jarvis.Api.Voice;

/// <summary>
/// <c>/jarvis/voice</c>: the WebSocket every listener speaks (the Mac bridge now, satellites later), protocol in
/// jarvis-voice-bridge.md section 2; with <c>?speech=local</c> text turns only (jarvis-satellites.md 2). The person is
/// the token's: an app token from "Połączone aplikacje" or a normal one. A household satellite's token names its
/// household (S13): the session is the satellite's, in that household, and a spoken PIN lends a turn to a person (S4). Only a device whose purpose is
/// <c>assistant</c> may open it (H-3c): Devices says so, a smart-home device's token is refused.
/// </summary>
internal sealed class VoiceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/jarvis/voice", async (
            HttpContext http, ICurrentUser user, IOptions<VoiceOptions> options, IHouseholdDevices householdDevices, IServiceScopeFactory scopes, ILogger<VoiceSession> logger) =>
        {
            // A listener with its own speech engine (S1) needs none on the server.
            var local = string.Equals(http.Request.Query["speech"], "local", StringComparison.Ordinal);
            if (!local && !options.Value.Enabled)
            {
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "voice.disabled",
                    detail: "JARVIS has no ears on this server (Jarvis:Voice:Enabled).");
            }

            if (!http.WebSockets.IsWebSocketRequest)
            {
                return Results.BadRequest();
            }

            Guid? satellite = Guid.TryParse(http.User.FindFirst(ClaimNames.Household)?.Value, out var household) ? household : null;
            if (satellite is { } satelliteHousehold)
            {
                // H-3c: only an assistant listens and speaks; a smart-home device's token (same household claim) is refused.
                if (await householdDevices.ListAsync(satelliteHousehold, http.RequestAborted) is not { } devices)
                {
                    return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "voice.devices_unavailable",
                        detail: "The household's devices could not be checked; try again in a moment.");
                }

                if (!devices.Any(d => d.Id == user.RequiredUserId && d.IsAssistant))
                {
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "voice.not_assistant",
                        detail: "Only a device whose purpose is assistant may use the voice channel.");
                }
            }

            using var socket = await http.WebSockets.AcceptWebSocketAsync();
            using var session = new VoiceSession(socket, user.RequiredUserId, satellite, local, scopes, options.Value, logger);
            await session.RunAsync(http.RequestAborted);
            return Results.Empty;
        }).WithTags("JARVIS");
    }
}

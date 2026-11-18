using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Jarvis.Application.Voice;
using Microsoft.Extensions.Options;

namespace Jarvis.Api.Voice;

/// <summary>
/// <c>/jarvis/voice</c>: the WebSocket every listener speaks (the Mac bridge now, satellites later), protocol in
/// jarvis-voice-bridge.md section 2; with <c>?speech=local</c> text turns only (jarvis-satellites.md 2). The person is
/// the token's: an app token from "Połączone aplikacje" or a normal one.
/// </summary>
internal sealed class VoiceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/jarvis/voice", async (
            HttpContext http, ICurrentUser user, IOptions<VoiceOptions> options, IServiceScopeFactory scopes, ILogger<VoiceSession> logger) =>
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

            using var socket = await http.WebSockets.AcceptWebSocketAsync();
            using var session = new VoiceSession(socket, user.RequiredUserId, local, scopes, options.Value, logger);
            await session.RunAsync(http.RequestAborted);
            return Results.Empty;
        }).WithTags("JARVIS");
    }
}

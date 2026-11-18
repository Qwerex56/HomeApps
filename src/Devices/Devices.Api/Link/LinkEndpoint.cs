using HomeApps.BuildingBlocks.Api.Endpoints;

namespace Devices.Api.Link;

/// <summary>
/// <c>/devices/link</c>: the control socket of a device (jarvis-satellites.md 2a). The owner is the token's person: an app
/// token from "Połączone aplikacje" (JARVIS's resource) or a normal one. Speech goes to <c>/jarvis/voice</c>, not here.
/// </summary>
internal sealed class LinkEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/devices/link", async (
            HttpContext http, DeviceLinks links, IServiceScopeFactory scopes, ILogger<LinkSession> logger) =>
        {
            if (!http.WebSockets.IsWebSocketRequest)
            {
                return Results.BadRequest();
            }

            using var socket = await http.WebSockets.AcceptWebSocketAsync();
            var session = new LinkSession(socket, links, scopes, logger);
            await session.RunAsync(http.RequestAborted);
            return Results.Empty;
        }).WithTags("Devices");
    }
}

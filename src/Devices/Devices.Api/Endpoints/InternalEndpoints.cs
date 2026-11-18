using System.Security.Cryptography;
using System.Text;
using Devices.Application.Common;
using Devices.Application.Tools.JarvisDevices;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Devices.Api.Endpoints;

/// <summary>
/// Service to service, inside the Docker network (like Households' <c>/internal/*</c>): the JARVIS service client proves itself with its
/// secret (<c>Devices:Internal:ClientId</c>/<c>ClientSecret</c>, sent as <c>X-Client-Id</c>/<c>X-Client-Secret</c>, compared in constant time).
/// Not reachable through the gateway (Traefik routes only <c>/devices</c>); no secret configured means always 401.
/// </summary>
internal sealed class InternalEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        // H-3c: the household's devices for JARVIS's prompt section, for one person (?user={guid}; members and the household's own devices only) (and its check that a satellite is an assistant).
        app.MapGet("/internal/households/{id:guid}/devices", async (Guid id, Guid? user, HttpRequest request, IConfiguration configuration, ISender sender, CancellationToken ct) =>
            !Authorized(request, configuration.GetSection("Devices:Internal"))
                ? Results.Unauthorized()
                : user is not { } person || person == Guid.Empty
                    ? Results.BadRequest()
                    : (await sender.Send(new GetJarvisDevicesQuery(id, person), ct)).ToHttpResult())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<IReadOnlyList<DeviceDescriptionView>>();

    private static bool Authorized(HttpRequest request, IConfigurationSection client)
    {
        var clientId = client["ClientId"];
        var secret = client["ClientSecret"];
        return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(secret)
            && Same(clientId, request.Headers["X-Client-Id"].ToString()) && Same(secret, request.Headers["X-Client-Secret"].ToString());
    }

    private static bool Same(string expected, string given) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(given)));
}

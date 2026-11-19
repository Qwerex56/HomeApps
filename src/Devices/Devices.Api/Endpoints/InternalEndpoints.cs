using System.Security.Cryptography;
using System.Text;
using Devices.Api.Requests;
using Devices.Application.Common;
using Devices.Application.Controls.SetControlForAddon;
using Devices.Application.Tools.JarvisDevices;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Devices.Api.Endpoints;

/// <summary>
/// Service to service, inside the Docker network (like Households' <c>/internal/*</c>): the JARVIS service client proves itself with its
/// secret (<c>Devices:Internal:ClientId</c>/<c>ClientSecret</c>, sent as <c>X-Client-Id</c>/<c>X-Client-Secret</c>, compared in constant time).
/// HomeMix's client (<c>Devices:Internal:HomeMix:*</c>) may read the household's devices the same way and, for its add-ons' triggers (D8), set a control.
/// Not reachable through the gateway (Traefik routes only <c>/devices</c>); no secret configured means always 401.
/// </summary>
internal sealed class InternalEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // H-3c: the household's devices for JARVIS's prompt section, for one person (?user={guid}; members and the household's own devices only) (and its check that a satellite is an assistant).
        app.MapGet("/internal/households/{id:guid}/devices", async (Guid id, Guid? user, HttpRequest request, IConfiguration configuration, ISender sender, CancellationToken ct) =>
            !(Authorized(request, configuration.GetSection("Devices:Internal")) || Authorized(request, configuration.GetSection(HomeMixSection)))
                ? Results.Unauthorized()
                : user is not { } person || person == Guid.Empty
                    ? Results.BadRequest()
                    : (await sender.Send(new GetJarvisDevicesQuery(id, person), ct)).ToHttpResult())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<IReadOnlyList<DeviceDescriptionView>>();

        // H-4b: an add-on's trigger changes a control as the add-on, within the installer's Admin+ rights; the add-on's consent is checked by HomeMix.
        app.MapPost("/internal/devices/{id:guid}/set", async (Guid id, AddonSetRequest body, HttpRequest request, IConfiguration configuration, ISender sender, CancellationToken ct) =>
            !Authorized(request, configuration.GetSection(HomeMixSection))
                ? Results.Unauthorized()
                : (await sender.Send(new SetControlForAddonCommand(
                    id, body.HouseholdId, body.Control, body.Value, body.InstallerUserId, body.InstallationId, body.AddonName), ct)).ToHttpResult())
            .AllowAnonymous()
            .WithTags("Internal");
    }

    private const string HomeMixSection = "Devices:Internal:HomeMix";

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

using Devices.Application.Capabilities;
using Devices.Application.Capabilities.ListRegisteredCapabilities;
using Devices.Application.Capabilities.SetCapabilityHidden;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Devices.Api.Endpoints;

/// <summary>
/// The namespaced capabilities that devices brought into this instance (D14), for the server's Owner: the list and
/// hide or show. A capability is promoted to a built-in by a release, not here.
/// </summary>
internal sealed class CapabilitiesEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapVersionedGroup("/devices").MapGroup("/admin/capabilities").WithTags("Devices");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListRegisteredCapabilitiesQuery(), ct)).ToHttpResult())
            .Produces<IReadOnlyList<RegisteredCapabilityView>>();

        group.MapPost("/{author}/{name}/hide", async (string author, string name, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetCapabilityHiddenCommand($"{author}/{name}", true), ct)).ToHttpResult());

        group.MapPost("/{author}/{name}/unhide", async (string author, string name, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetCapabilityHiddenCommand($"{author}/{name}", false), ct)).ToHttpResult());
    }
}

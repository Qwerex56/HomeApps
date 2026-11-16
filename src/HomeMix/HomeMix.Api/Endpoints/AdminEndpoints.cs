using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Http;
using HomeMix.Api.Requests;
using HomeMix.Application.Blocks;
using HomeMix.Application.Blocks.BlockAddon;
using HomeMix.Application.Blocks.ListBlocks;
using HomeMix.Application.Blocks.UnblockAddon;
using HomeMix.Application.Installations.ListAllInstallations;
using HomeMix.Application.Store;
using HomeMix.Application.Store.HideListing;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// The Owner's "Dodatki" view (A5): every installation on the instance, the blocks and hiding store entries. Only the server role Owner, the
/// person who hosts HomeApps (D9); an assistant never.
/// </summary>
internal sealed class AdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/homemix/api/admin").WithTags("Administration").RequireAuthorization(Policies.Owner);

        admin.MapGet("/installations", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListAllInstallationsQuery(), ct)).ToHttpResult())
            .Produces<IReadOnlyList<AdminInstallationView>>();

        admin.MapGet("/blocks", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListBlocksQuery(), ct)).ToHttpResult())
            .Produces<IReadOnlyList<BlockView>>();

        admin.MapPut("/blocks/{value}", async (string value, BlockRequest? body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new BlockAddonCommand(value, body?.Reason), ct)).ToHttpResult())
            .Produces<BlockView>();

        admin.MapDelete("/blocks/{value}", async (string value, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UnblockAddonCommand(value), ct)).ToHttpResult());

        // Hides a store entry or shows it again (homemix-2.md 2.2); blocks still stop the add-on itself.
        admin.MapPut("/store/{key}/hidden", async (string key, HideListingRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new HideListingCommand(key, body.Hidden), ct)).ToHttpResult())
            .Produces<StoreEntryView>();
    }
}

using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Application.Packages;
using HomeMix.Application.Packages.AddOfficialPackage;
using HomeMix.Application.Store;
using HomeMix.Api.Requests;
using HomeMix.Application.Store.GetListing;
using HomeMix.Application.Store.GetMyListings;
using HomeMix.Application.Store.GiveHeart;
using HomeMix.Application.Store.TakeHeart;
using HomeMix.Application.Store.GetStore;
using HomeMix.Application.Store.RefreshListing;
using HomeMix.Application.Store.RemoveListing;
using HomeMix.Application.Store.SubmitListing;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// The store (homemix-2.md 2.1): add-ons built into HomeMix, official repositories of addons.json and repositories
/// people submitted. An entry is installed like any package: built-in ones by id here, the others through
/// <c>POST /homemix/api/packages/github</c>. An assistant may read the store but not submit or change entries or give "domki".
/// </summary>
internal sealed class StoreEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/homemix/api/store", async (
                    string? tab, string? q, string? target, bool? jarvis, bool? offline, int? page, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetStoreQuery(tab, q, target, jarvis ?? false, offline ?? false, page ?? 1), ct)).ToHttpResult())
            .WithTags("Store")
            .Produces<IReadOnlyList<StoreEntryView>>();

        app.MapGet("/homemix/api/store/mine", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyListingsQuery(), ct)).ToHttpResult())
            .WithTags("Store")
            .Produces<IReadOnlyList<MyListingView>>();

        app.MapGet("/homemix/api/store/{key}", async (string key, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetListingQuery(key), ct)).ToHttpResult())
            .WithTags("Store")
            .Produces<StoreEntryView>();

        app.MapPost("/homemix/api/store", async (SubmitListingRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SubmitListingCommand(body.Repository), ct)).ToHttpResult())
            .WithTags("Store")
            .Produces<StoreEntryView>();

        app.MapPost("/homemix/api/store/{key}/refresh", async (string key, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RefreshListingCommand(key), ct)).ToHttpResult())
            .WithTags("Store")
            .Produces<StoreEntryView>();

        app.MapDelete("/homemix/api/store/{key}", async (string key, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveListingCommand(key), ct)).ToHttpResult())
            .WithTags("Store");

        app.MapPut("/homemix/api/store/{key}/heart", async (string key, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GiveHeartCommand(key), ct)).ToHttpResult())
            .WithTags("Store");

        app.MapDelete("/homemix/api/store/{key}/heart", async (string key, ISender sender, CancellationToken ct) =>
                (await sender.Send(new TakeHeartCommand(key), ct)).ToHttpResult())
            .WithTags("Store");

        app.MapPost("/homemix/api/packages/official/{addonId}", async (string addonId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddOfficialPackageCommand(addonId), ct)).ToHttpResult())
            .WithTags("Packages")
            .Produces<PackageView>();
    }
}

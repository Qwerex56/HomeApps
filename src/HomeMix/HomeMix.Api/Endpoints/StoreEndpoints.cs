using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Application.Packages;
using HomeMix.Application.Packages.AddOfficialPackage;
using HomeMix.Application.Store;
using HomeMix.Application.Store.GetStore;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// The store (W9): official add-ons that came with HomeMix and the index's GitHub repositories. An entry is installed
/// like any package: official ones by id here, the others through <c>POST /homemix/api/packages/github</c>.
/// </summary>
internal sealed class StoreEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/homemix/api/store", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetStoreQuery(), ct)).ToHttpResult())
            .WithTags("Store")
            .Produces<IReadOnlyList<StoreEntryView>>();

        app.MapPost("/homemix/api/packages/official/{addonId}", async (string addonId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AddOfficialPackageCommand(addonId), ct)).ToHttpResult())
            .WithTags("Packages")
            .Produces<PackageView>();
    }
}

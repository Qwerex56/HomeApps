using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Requests;
using HomeMix.Application.Installations;
using HomeMix.Application.Installations.GetHouseholdInstallations;
using HomeMix.Application.Installations.GetInstallation;
using HomeMix.Application.Installations.GetMyAccess;
using HomeMix.Application.Installations.GetMyInstallations;
using HomeMix.Application.Installations.Install;
using HomeMix.Application.Installations.RemoveInstallation;
using HomeMix.Application.Installations.UpdateInstallation;
using HomeMix.Domain.Installations;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// Add-ons in a household (role Admin and up installs, every member sees them) or on the caller's account. An
/// assistant acting for a person (JARVIS) reads these but never installs, updates or removes (the kit's default for a
/// core service).
/// </summary>
internal sealed class InstallationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/homemix/api").WithTags("Installations");

        api.MapGet("/households/{householdId:guid}/installations", async (Guid householdId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetHouseholdInstallationsQuery(householdId), ct)).ToHttpResult())
            .Produces<IReadOnlyList<InstallationView>>();

        api.MapPost("/households/{householdId:guid}/installations", async (Guid householdId, InstallRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new InstallCommand(body.Sha256, TargetKind.Household, householdId, body.Consent), ct))
                .ToCreatedHttpResult(view => $"/homemix/api/installations/{view.Id}"))
            .Produces<InstallationView>(StatusCodes.Status201Created);

        api.MapGet("/me/installations", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyInstallationsQuery(), ct)).ToHttpResult())
            .Produces<IReadOnlyList<InstallationView>>();

        api.MapPost("/me/installations", async (InstallRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new InstallCommand(body.Sha256, TargetKind.User, null, body.Consent), ct))
                .ToCreatedHttpResult(view => $"/homemix/api/installations/{view.Id}"))
            .Produces<InstallationView>(StatusCodes.Status201Created);

        api.MapGet("/installations/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetInstallationQuery(id), ct)).ToHttpResult())
            .Produces<InstallationView>();

        api.MapGet("/installations/{id:guid}/me", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyAccessQuery(id), ct)).ToHttpResult())
            .Produces<MyAccessView>();

        api.MapPut("/installations/{id:guid}", async (Guid id, UpdateInstallationRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateInstallationCommand(id, body.Sha256, body.Consent), ct)).ToHttpResult())
            .Produces<InstallationView>();

        api.MapDelete("/installations/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RemoveInstallationCommand(id), ct)).ToHttpResult());
    }
}

using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeMix.Api.Requests;
using HomeMix.Application.Triggers;
using HomeMix.Application.Triggers.ListHouseholdTriggers;
using HomeMix.Application.Triggers.SetTriggerEnabled;
using MediatR;

namespace HomeMix.Api.Endpoints;

/// <summary>
/// The server-side triggers of a household's add-ons (docs/homemix.md, "Triggers"): the household's admins see what they react to and
/// how they are doing, and switch them off or on again (the JARVIS tab's list, UI in H-9).
/// </summary>
internal sealed class TriggerEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/homemix/api").WithTags("Triggers");

        api.MapGet("/households/{householdId:guid}/triggers", async (Guid householdId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListHouseholdTriggersQuery(householdId), ct)).ToHttpResult())
            .Produces<IReadOnlyList<TriggerView>>();

        api.MapPut("/installations/{id:guid}/triggers/{triggerId}", async (Guid id, string triggerId, SetTriggerEnabledRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SetTriggerEnabledCommand(id, triggerId, body.Enabled), ct)).ToHttpResult())
            .Produces<TriggerView>();
    }
}

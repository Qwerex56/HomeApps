using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Jarvis.Api.Requests;
using Jarvis.Application.Actions.Decide;
using Jarvis.Application.Common;
using Jarvis.Application.Runs.Ask;
using MediatR;

namespace Jarvis.Api.Endpoints;

/// <summary>
/// JARVIS without a channel (jarvis-v1.md 7): ask and get the answer with its actions, and decide on an action that
/// waits for a confirmation. A person's own token only: an assistant's is refused by the kit's guard.
/// </summary>
internal sealed class AssistantEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/jarvis/api").WithTags("JARVIS");

        api.MapPost("/ask", async (AskRequest body, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AskCommand(user.RequiredUserId, body.Text, body.HouseholdId, "app", null, []), ct)).ToHttpResult())
            .Produces<AnswerView>();

        api.MapPost("/confirmations/{id:guid}", async (Guid id, DecisionRequest body, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DecideCommand(user.RequiredUserId, id, body.Approve), ct)).ToHttpResult())
            .Produces<ActionView>();
    }
}

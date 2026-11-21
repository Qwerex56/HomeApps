using FluentResults;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using Jarvis.Api.Requests;
using Jarvis.Application.Actions.Decide;
using Jarvis.Application.Actions.ListPendingConfirmations;
using Jarvis.Application.Common;
using Jarvis.Application.Runs.Ask;
using MediatR;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Jarvis.Api.Endpoints;

/// <summary>
/// JARVIS without a channel (jarvis-v1.md 7): ask and get the answer with its actions, and decide on an action that
/// waits for a confirmation. A person's own token only: an assistant's is refused by the kit's guard, and connected
/// external apps cannot list or decide confirmations.
/// </summary>
internal sealed class AssistantEndpoints : IEndpoint
{
    private static readonly AppError ConnectedAppForbidden = new(
        "jarvis.forbidden",
        "Connected external apps cannot access confirmations.",
        ErrorKind.Forbidden);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/jarvis/api").WithTags("JARVIS");

        api.MapPost("/ask", async (AskRequest body, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new AskCommand(user.RequiredUserId, body.Text, body.HouseholdId, "app", null, []), ct)).ToHttpResult())
            .Produces<AnswerView>();

        api.MapGet("/confirmations", async (bool? pending, HttpContext http, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            if (IsConnectedApp(http))
            {
                return Result.Fail(ConnectedAppForbidden).ToHttpResult();
            }

            if (pending.HasValue && !pending.Value)
            {
                return HttpResults.Ok(Array.Empty<ActionView>());
            }

            return (await sender.Send(new ListPendingConfirmationsQuery(user.RequiredUserId), ct)).ToHttpResult();
        })
        .Produces<IReadOnlyList<ActionView>>();

        api.MapPost("/confirmations/{id:guid}", async (Guid id, DecisionRequest body, HttpContext http, ICurrentUser user, ISender sender, CancellationToken ct) =>
        {
            if (IsConnectedApp(http))
            {
                return Result.Fail(ConnectedAppForbidden).ToHttpResult();
            }

            return (await sender.Send(new DecideCommand(user.RequiredUserId, id, body.Approve), ct)).ToHttpResult();
        })
        .Produces<ActionView>();
    }

    private static bool IsConnectedApp(HttpContext context) =>
        context.User.HasClaim(c => c.Type == HomeApps.BuildingBlocks.Api.Auth.ClaimNames.ClientId);
}

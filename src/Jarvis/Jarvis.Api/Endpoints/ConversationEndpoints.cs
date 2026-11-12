using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using Jarvis.Api.Requests;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Application.Conversations.DeleteConversation;
using Jarvis.Application.Conversations.ListConversations;
using Jarvis.Application.Conversations.ReadConversation;
using Jarvis.Application.Conversations.SendMessage;
using Jarvis.Application.Conversations.StartConversation;
using Jarvis.Infrastructure.Live;
using MediatR;

namespace Jarvis.Api.Endpoints;

/// <summary>
/// The person's conversations with JARVIS in the app (jarvis-v1.md 2.4). Sending a message answers with server-sent
/// events: <c>text</c> pieces as the model writes, <c>action</c> for each card line as the gateway logs it, then one
/// <c>message</c> (JARVIS's kept answer with its card) or <c>error</c> (<c>{code, detail}</c>).
/// </summary>
internal sealed class ConversationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/jarvis/api/conversations").WithTags("JARVIS");

        api.MapGet("/", async (ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListConversationsQuery(user.RequiredUserId), ct)).ToHttpResult())
            .Produces<IReadOnlyList<ConversationView>>();

        api.MapPost("/", async (StartConversationRequest body, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new StartConversationCommand(user.RequiredUserId, body.Title), ct)).ToHttpResult())
            .Produces<ConversationView>();

        api.MapDelete("/{id:guid}", async (Guid id, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DeleteConversationCommand(user.RequiredUserId, id), ct)).ToHttpResult());

        api.MapGet("/{id:guid}/messages", async (Guid id, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ReadConversationQuery(user.RequiredUserId, id), ct)).ToHttpResult())
            .Produces<IReadOnlyList<MessageView>>();

        api.MapPost("/{id:guid}/messages", (Guid id, SendMessageRequest body, ICurrentUser user, ISender sender, RunFeed feed, CancellationToken ct) =>
            TypedResults.ServerSentEvents(Answer(new SendMessageCommand(user.RequiredUserId, id, body.Text, body.HouseholdId, Guid.NewGuid()), sender, feed, ct)))
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");
    }

    private static async IAsyncEnumerable<SseItem<object>> Answer(
        SendMessageCommand command, ISender sender, RunFeed feed, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamId = command.StreamId!.Value;
        var updates = feed.Open(streamId);
        var sending = SendAsync(command, sender, feed, cancellationToken);

        await foreach (var update in updates.ReadAllAsync(cancellationToken))
        {
            yield return update switch
            {
                RunText text => new SseItem<object>(text.Text, "text"),
                RunAction action => new SseItem<object>(action.Action, "action"),
                _ => throw new InvalidOperationException($"Unknown update {update.GetType().Name}."),
            };
        }

        var answer = await sending;
        if (answer.IsSuccess)
        {
            yield return new SseItem<object>(answer.Value, "message");
        }
        else
        {
            var error = answer.Errors.OfType<AppError>().FirstOrDefault();
            yield return new SseItem<object>(new { code = error?.Code ?? "jarvis.error", detail = error?.Message ?? "Something went wrong." }, "error");
        }
    }

    /// <summary>Runs while the stream is read; closing the stream ends the reading loop with the last update in.</summary>
    private static async Task<FluentResults.Result<MessageView>> SendAsync(
        SendMessageCommand command, ISender sender, RunFeed feed, CancellationToken cancellationToken)
    {
        try
        {
            return await sender.Send(command, cancellationToken);
        }
        finally
        {
            feed.Close(command.StreamId!.Value);
        }
    }
}

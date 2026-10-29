using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace HomeApps.Plugins.Hello;

internal sealed class GreetingEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var greetings = app.MapGroup("/households/{householdId:guid}/greetings").WithTags("Greetings");

        greetings.MapGet("/", async (Guid householdId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListGreetingsQuery(householdId), ct)).ToHttpResult())
            .Produces<IReadOnlyList<GreetingView>>();

        greetings.MapPost("/", async (Guid householdId, SayHelloRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SayHelloCommand(householdId, body.Text), ct)).ToHttpResult());
    }
}

internal sealed record SayHelloRequest(string Text);

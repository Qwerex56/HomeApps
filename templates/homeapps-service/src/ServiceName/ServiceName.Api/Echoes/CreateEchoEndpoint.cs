using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using ServiceName.Application.Echoes.CreateEcho;

namespace ServiceName.Api.Echoes;

internal sealed class CreateEchoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/echoes", async (CreateEchoCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToCreatedHttpResult(id => $"/echoes/{id}"))
            .WithTags("Echoes")
            .WithName("CreateEcho");
    }
}

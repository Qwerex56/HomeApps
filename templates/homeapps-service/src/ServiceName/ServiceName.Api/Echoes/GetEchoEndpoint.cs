using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using ServiceName.Application.Echoes.GetEcho;

namespace ServiceName.Api.Echoes;

internal sealed class GetEchoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/echoes/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetEchoQuery(id), ct)).ToHttpResult())
            .WithTags("Echoes")
            .WithName("GetEcho");
    }
}

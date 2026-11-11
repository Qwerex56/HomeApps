using Auth.Application.Delegation.DelegateToAssistant;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Auth.Api.Endpoints;

internal sealed record DelegationRequest(string ClientId, string ClientSecret, Guid UserId, string Channel, string? Reference);

/// <summary>
/// jarvis-v1.md, section 3: a short token for an assistant acting for one person. Anonymous for the token pipeline,
/// the client proves itself with its secret. Traefik routes only <c>/auth</c> and <c>/.well-known</c> to Auth, so
/// <c>/internal</c> is reachable from the Docker network only.
/// </summary>
internal sealed class DelegationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/delegation", async (DelegationRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(
                new DelegateToAssistantCommand(body.ClientId, body.ClientSecret, body.UserId, body.Channel, body.Reference), ct))
            .ToHttpResult())
            .AllowAnonymous()
            .WithTags("Internal")
            .Produces<DelegatedTokenView>();
    }
}

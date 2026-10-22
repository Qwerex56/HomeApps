using Auth.Application.Admin;
using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;

namespace Auth.Api.Endpoints;

internal sealed record ChangeRoleRequest(ServerRole Role);

/// <summary>A-22, A-23. The slices check the caller's role (R10, R18), so the endpoints only need a signed-in user.</summary>
internal sealed class AdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var identities = app.MapGroup("/auth/admin/identities").WithTags("Administration");

        identities.MapPost("/{id:guid}/password-reset", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ResetPasswordForCommand(id), ct)).ToHttpResult());

        identities.MapPut("/{id:guid}/role", async (Guid id, ChangeRoleRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ChangeRoleCommand(id, body.Role), ct)).ToHttpResult());
    }
}

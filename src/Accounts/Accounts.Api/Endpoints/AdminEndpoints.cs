using Accounts.Api.Http;
using Accounts.Application.Abstractions;
using Accounts.Application.Admin;
using Accounts.Application.Common;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounts.Api.Endpoints;

internal sealed record EraseRequest(string ConfirmEmail);

/// <summary>
/// UC-10 to UC-17. The group lets in only Admin and Owner; the slices and the domain still decide who may change whom
/// (R1–R5), so a role check here is never the only one.
/// </summary>
internal sealed class AdminEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/admin/users").WithTags("Administration").RequireAuthorization(Policies.Admin);

        users.MapGet("/", async (
            AccountStatus? status, ServerRole? role, string? q, int? page, int? pageSize, UserSort? sort, bool? desc,
            ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListUsersQuery(
                status, role, q, page ?? 1, pageSize ?? ListUsersQuery.DefaultPageSize, sort ?? UserSort.DisplayName, desc ?? false), ct))
            .ToHttpResult());

        users.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetUserQuery(id), ct)).ToHttpResult());

        users.MapPut("/{id:guid}/profile", async (Guid id, ProfileInput body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new UpdateUserProfileCommand(id, body), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        users.MapPost("/{id:guid}/disable", async (Guid id, DisableRequest? body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new DisableUserCommand(id, body?.Reason), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        users.MapPost("/{id:guid}/enable", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new EnableUserCommand(id), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        users.MapGet("/{id:guid}/history", async (Guid id, DateTimeOffset? before, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetUserHistoryQuery(id, before), ct)).ToHttpResult());

        // Q8: DELETE, with the confirmation in the body so the email never lands in an URL or an access log.
        users.MapDelete("/{id:guid}", async (Guid id, [FromBody] EraseRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new EraseUserCommand(id, body.ConfirmEmail), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);
    }
}

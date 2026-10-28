using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Results;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Households.Api.Http;
using Households.Application.Common;
using Households.Application.Members;
using Households.Domain.Households;
using MediatR;

namespace Households.Api.Endpoints;

internal sealed record NicknameRequest(string? Nickname);

internal sealed record RoleRequest(HouseholdRole Role);

internal sealed record OwnershipRequest(Guid UserId);

internal sealed record GuestAccessRequest(DateTimeOffset? Until);

/// <summary>M1–M6, S5 and guest access (D2c). <c>me</c> is always the caller from the token.</summary>
internal sealed class MemberEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var household = app.MapGroup("/households/{id:guid}").WithTags("Members");
        var members = household.MapGroup("/members");

        members.MapGet("/", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ListMembersQuery(id), ct)).ToHttpResult())
            .Produces<IReadOnlyList<MemberView>>();

        members.MapGet("/me", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyMembershipQuery(id), ct)).ToHttpResult())
            .Produces<MyMembershipView>();

        members.MapPut("/me/nickname", async (Guid id, NicknameRequest body, ICurrentUser user, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetNicknameCommand(id, user.RequiredUserId, body.Nickname), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        members.MapDelete("/me", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new LeaveHouseholdCommand(id), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        members.MapPut("/{userId:guid}/nickname", async (Guid id, Guid userId, NicknameRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetNicknameCommand(id, userId, body.Nickname), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        members.MapPut("/{userId:guid}/role", async (Guid id, Guid userId, RoleRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ChangeMemberRoleCommand(id, userId, body.Role), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        members.MapPut("/{userId:guid}/access", async (Guid id, Guid userId, GuestAccessRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new SetGuestAccessCommand(id, userId, body.Until), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        members.MapDelete("/{userId:guid}", async (Guid id, Guid userId, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RemoveMemberCommand(id, userId), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);

        household.MapPost("/ownership", async (Guid id, OwnershipRequest body, ISender sender, CancellationToken ct) =>
            (await sender.Send(new TransferOwnershipCommand(id, body.UserId), ct)).ToHttpResult())
            .RequireRateLimiting(RateLimits.Changes);
    }
}

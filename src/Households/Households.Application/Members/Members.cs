using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Members;

/// <summary>
/// M1: members and up and server admins; a guest gets 403 (D2a: the list is private), a stranger 404 (Q3).
/// Owner first, then by role and join date.
/// </summary>
public sealed record ListMembersQuery(Guid HouseholdId) : IQuery<IReadOnlyList<MemberView>>;

internal sealed class ListMembersHandler(IHouseholdRepository households, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListMembersQuery, IReadOnlyList<MemberView>>
{
    public async Task<Result<IReadOnlyList<MemberView>>> Handle(ListMembersQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<IReadOnlyList<MemberView>>();
        }

        var household = found.Value;
        var actor = currentUser.Actor();
        var now = time.GetUtcNow();
        if (!household.CanSeeMembers(actor, now))
        {
            return Result.Fail<IReadOnlyList<MemberView>>(
                household.CanRead(actor, now) ? HouseholdErrors.Forbidden : HouseholdErrors.NotFound);
        }

        var ids = household.Members.Select(m => m.UserId)
            .Concat(household.Members.Where(m => m.InvitedBy is not null).Select(m => m.InvitedBy!.Value))
            .Distinct()
            .ToList();
        var copies = await users.GetManyAsync(ids, cancellationToken);
        return household.Members
            .Where(m => m.HasAccess(now))
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m =>
            {
                copies.TryGetValue(m.UserId, out var user);
                var displayName = user?.DisplayName ?? Domain.Users.UserCopy.FallbackName;
                string? invitedByName = m.InvitedBy is { } by && copies.TryGetValue(by, out var inviter) ? inviter.DisplayName : null;
                return new MemberView(
                    m.UserId, m.Nickname?.Value ?? displayName, m.Nickname?.Value, displayName, user?.AvatarUrl, m.Role,
                    user?.IsActive ?? true, m.JoinedAt, m.InvitedBy, invitedByName, m.AccessUntil);
            })
            .ToList();
    }
}

/// <summary>S5: my membership; 404 when I have none (Q3).</summary>
public sealed record GetMyMembershipQuery(Guid HouseholdId) : IQuery<MyMembershipView>;

internal sealed class GetMyMembershipHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<GetMyMembershipQuery, MyMembershipView>
{
    public async Task<Result<MyMembershipView>> Handle(GetMyMembershipQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<MyMembershipView>();
        }

        var role = found.Value.RoleOf(currentUser.Actor(), time.GetUtcNow());
        if (role.IsFailed)
        {
            return role.ToResult<MyMembershipView>();
        }

        var me = found.Value.MemberOf(currentUser.RequiredUserId)!;
        return new MyMembershipView(found.Value.Id, me.Role, me.Nickname?.Value, me.JoinedAt, me.AccessUntil);
    }
}

/// <summary>M2.</summary>
public sealed record RemoveMemberCommand(Guid HouseholdId, Guid UserId) : ICommand;

internal sealed class RemoveMemberHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> Handle(RemoveMemberCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed ? household.ToResult() : household.Value.RemoveMember(currentUser.Actor(), command.UserId, time);
    }
}

/// <summary>M3.</summary>
public sealed record LeaveHouseholdCommand(Guid HouseholdId) : ICommand;

internal sealed class LeaveHouseholdHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<LeaveHouseholdCommand>
{
    public async Task<Result> Handle(LeaveHouseholdCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed ? household.ToResult() : household.Value.Leave(currentUser.Actor(), time);
    }
}

/// <summary>M4.</summary>
public sealed record ChangeMemberRoleCommand(Guid HouseholdId, Guid UserId, HouseholdRole Role) : ICommand;

internal sealed class ChangeMemberRoleValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleValidator() => RuleFor(c => c.Role).IsInEnum();
}

internal sealed class ChangeMemberRoleHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<ChangeMemberRoleCommand>
{
    public async Task<Result> Handle(ChangeMemberRoleCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.ChangeRole(currentUser.Actor(), command.UserId, command.Role, time);
    }
}

/// <summary>M5.</summary>
public sealed record TransferOwnershipCommand(Guid HouseholdId, Guid ToUserId) : ICommand;

internal sealed class TransferOwnershipHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<TransferOwnershipCommand>
{
    public async Task<Result> Handle(TransferOwnershipCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.TransferOwnership(currentUser.Actor(), command.ToUserId, time);
    }
}

/// <summary>M6 and D2a: my nickname, or a guest's set by the Owner or an Admin. Blank clears it (Q4).</summary>
public sealed record SetNicknameCommand(Guid HouseholdId, Guid UserId, string? Nickname) : ICommand;

internal sealed class SetNicknameHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<SetNicknameCommand>
{
    public async Task<Result> Handle(SetNicknameCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.SetNickname(currentUser.Actor(), command.UserId, command.Nickname, time);
    }
}

/// <summary>D2c: when a guest's access ends; null = no end.</summary>
public sealed record SetGuestAccessCommand(Guid HouseholdId, Guid UserId, DateTimeOffset? Until) : ICommand;

internal sealed class SetGuestAccessHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<SetGuestAccessCommand>
{
    public async Task<Result> Handle(SetGuestAccessCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.SetGuestAccess(currentUser.Actor(), command.UserId, command.Until, time);
    }
}

/// <summary>D2c: run hourly by Infrastructure; removes every guest whose access ended. Returns how many.</summary>
public sealed record RemoveExpiredGuestsCommand : ICommand<int>;

internal sealed class RemoveExpiredGuestsHandler(IHouseholdRepository households, TimeProvider time)
    : ICommandHandler<RemoveExpiredGuestsCommand, int>
{
    public async Task<Result<int>> Handle(RemoveExpiredGuestsCommand command, CancellationToken cancellationToken)
    {
        var expired = await households.ListWithExpiredGuestsAsync(time.GetUtcNow(), cancellationToken);
        return expired.Sum(h => h.RemoveExpiredGuests(time));
    }
}

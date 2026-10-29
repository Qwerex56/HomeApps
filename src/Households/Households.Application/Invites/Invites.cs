using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Invites;

/// <summary>I1, D3: my own invite link, Member or Guest, valid for a chosen time; a guest invite may end the guest's access (D2c).</summary>
public sealed record CreateInviteCommand(
    Guid HouseholdId, HouseholdRole Role, InviteValidity Validity, DateTimeOffset? GuestAccessUntil) : ICommand<InviteView>;

internal sealed class CreateInviteValidator : AbstractValidator<CreateInviteCommand>
{
    public CreateInviteValidator()
    {
        RuleFor(c => c.Role).IsInEnum();
        RuleFor(c => c.Validity).IsInEnum();
    }
}

internal sealed class CreateInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<CreateInviteCommand, InviteView>
{
    public async Task<Result<InviteView>> Handle(CreateInviteCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult<InviteView>();
        }

        var invite = household.Value.CreateInvite(currentUser.Actor(), command.Role, command.Validity, command.GuestAccessUntil, time);
        if (invite.IsFailed)
        {
            return invite.ToResult<InviteView>();
        }

        invites.Add(invite.Value);
        var me = await users.GetAsync(currentUser.RequiredUserId, cancellationToken);
        return invite.Value.ToView(me?.DisplayName);
    }
}

/// <summary>I2, D3a: invites still working; everyone's for the Owner and Admins, only my own for a member.</summary>
public sealed record ListInvitesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<InviteView>>;

internal sealed class ListInvitesHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<ListInvitesQuery, IReadOnlyList<InviteView>>
{
    public async Task<Result<IReadOnlyList<InviteView>>> Handle(ListInvitesQuery query, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult<IReadOnlyList<InviteView>>();
        }

        var visible = household.Value.InvitesVisibleTo(currentUser.Actor(), time);
        if (visible.IsFailed)
        {
            return visible.ToResult<IReadOnlyList<InviteView>>();
        }

        var now = time.GetUtcNow();
        var list = (await invites.ListNotRevokedAsync(household.Value.Id, visible.Value, cancellationToken))
            .Where(i => i.IsUsable(now))
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
        var authors = await users.GetManyAsync(
            list.Where(i => i.CreatedBy is not null).Select(i => i.CreatedBy!.Value).Distinct().ToList(), cancellationToken);
        return list
            .Select(i => i.ToView(i.CreatedBy is { } by && authors.TryGetValue(by, out var author) ? author.DisplayName : null))
            .ToList();
    }
}

/// <summary>I3.</summary>
public sealed record RevokeInviteCommand(Guid HouseholdId, Guid InviteId) : ICommand;

internal sealed class RevokeInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RevokeInviteCommand>
{
    public async Task<Result> Handle(RevokeInviteCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult();
        }

        var invite = await invites.GetByIdAsync(command.InviteId, cancellationToken);
        if (invite is null)
        {
            var role = household.Value.RoleOf(currentUser.Actor(), time.GetUtcNow());
            return role.IsFailed ? role.ToResult() : Result.Fail(InviteErrors.NotFound);
        }

        return household.Value.RevokeInvite(currentUser.Actor(), invite, time);
    }
}

/// <summary>
/// I4: what a link leads to, for anyone signed in. One link format for both (Q1): an invite code first, then the
/// household's guest link (D2b). Unknown, expired or revoked: 404, nothing is revealed (Q3).
/// </summary>
public sealed record PreviewInviteQuery(string Code) : IQuery<InvitePreviewView>;

internal sealed class PreviewInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, TimeProvider time)
    : IQueryHandler<PreviewInviteQuery, InvitePreviewView>
{
    public async Task<Result<InvitePreviewView>> Handle(PreviewInviteQuery query, CancellationToken cancellationToken)
    {
        var target = await households.ResolveAsync(invites, query.Code, time.GetUtcNow(), cancellationToken);
        if (target.IsFailed)
        {
            return target.ToResult<InvitePreviewView>();
        }

        var (household, invite) = target.Value;
        if (invite is null)
        {
            return new InvitePreviewView(household.Id, household.Name.Value, HouseholdRole.Guest, null, null);
        }

        var author = invite.CreatedBy is { } by ? await users.GetAsync(by, cancellationToken) : null;
        return new InvitePreviewView(household.Id, household.Name.Value, invite.Role, author?.DisplayName, invite.ExpiresAt);
    }
}

/// <summary>I5 and D2b: join through an invite or the guest link; returns the household id. A disabled account cannot (Q5).</summary>
public sealed record AcceptInviteCommand(string Code) : ICommand<Guid>;

internal sealed class AcceptInviteHandler(
    IHouseholdRepository households, IInviteRepository invites, IUserCopies users, ICurrentUser currentUser,
    HouseholdSettings settings, TimeProvider time)
    : ICommandHandler<AcceptInviteCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AcceptInviteCommand command, CancellationToken cancellationToken)
    {
        var target = await households.ResolveAsync(invites, command.Code, time.GetUtcNow(), cancellationToken);
        if (target.IsFailed)
        {
            return target.ToResult<Guid>();
        }

        var active = await users.EnsureActiveAsync(currentUser.RequiredUserId, cancellationToken);
        if (active.IsFailed)
        {
            return active.ToResult<Guid>();
        }

        var (household, invite) = target.Value;
        var joined = invite is null
            ? household.JoinAsGuest(currentUser.RequiredUserId, household.GuestCode!, settings.MaxMembers, time)
            : household.Accept(invite, currentUser.RequiredUserId, settings.MaxMembers, time);
        return joined.IsFailed ? joined.ToResult<Guid>() : household.Id;
    }
}

internal static class InviteExtensions
{
    public static InviteView ToView(this Invite invite, string? createdByName) =>
        new(invite.Id, invite.Code.Display, invite.Role, invite.CreatedAt, invite.ExpiresAt, invite.GuestAccessUntil, invite.Uses,
            invite.CreatedBy, createdByName);

    /// <summary>
    /// The active household a code leads to, with the invite when it is an invite code and null for the guest link.
    /// Anything else is <c>invite.not_found</c>.
    /// </summary>
    public static async Task<Result<(Household Household, Invite? Invite)>> ResolveAsync(
        this IHouseholdRepository households, IInviteRepository invites, string code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var parsed = InviteCode.Parse(code);
        if (parsed.IsFailed)
        {
            return parsed.ToResult<(Household, Invite?)>();
        }

        var invite = await invites.GetByCodeAsync(parsed.Value, cancellationToken);
        var household = invite is not null
            ? await households.GetByIdAsync(invite.HouseholdId, cancellationToken)
            : await households.GetByGuestCodeAsync(parsed.Value, cancellationToken);
        if (household is not { Status: HouseholdStatus.Active } || (invite is not null && !invite.IsUsable(now)))
        {
            return Result.Fail<(Household, Invite?)>(InviteErrors.NotFound);
        }

        return (household, invite);
    }
}

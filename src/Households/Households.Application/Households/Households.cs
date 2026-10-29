using Households.Application.Abstractions;
using Households.Application.Common;
using Households.Domain.Households;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Households.Application.Households;

/// <summary>H1: the caller creates a household and becomes its Owner. A disabled account cannot (Q5).</summary>
public sealed record CreateHouseholdCommand(string Name, string? Description) : ICommand<Guid>;

internal sealed class CreateHouseholdValidator : AbstractValidator<CreateHouseholdCommand>
{
    public CreateHouseholdValidator() => RuleFor(c => c.Name).NotNull();
}

internal sealed class CreateHouseholdHandler(
    IHouseholdRepository households, IUserCopies users, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<CreateHouseholdCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateHouseholdCommand command, CancellationToken cancellationToken)
    {
        var active = await users.EnsureActiveAsync(currentUser.RequiredUserId, cancellationToken);
        if (active.IsFailed)
        {
            return active.ToResult<Guid>();
        }

        var household = Household.Create(Guid.CreateVersion7(time.GetUtcNow()), currentUser.RequiredUserId, command.Name, command.Description, time);
        if (household.IsFailed)
        {
            return household.ToResult<Guid>();
        }

        households.Add(household.Value);
        return household.Value.Id;
    }
}

/// <summary>H2: my households with my role (Q7: no paging). Guests do not see the member count (D2a).</summary>
public sealed record GetMyHouseholdsQuery : IQuery<IReadOnlyList<MyHouseholdView>>;

internal sealed class GetMyHouseholdsHandler(IHouseholdDirectory directory, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<GetMyHouseholdsQuery, IReadOnlyList<MyHouseholdView>>
{
    public async Task<Result<IReadOnlyList<MyHouseholdView>>> Handle(GetMyHouseholdsQuery query, CancellationToken cancellationToken)
    {
        var items = await directory.MyHouseholdsAsync(currentUser.RequiredUserId, time.GetUtcNow(), cancellationToken);
        return items
            .Select(i => new MyHouseholdView(
                i.Id, i.Name, i.Description, i.MyRole,
                i.MyRole >= HouseholdRole.Member ? i.MemberCount : null,
                i.MyRole == HouseholdRole.Owner && i.OwnershipReceivedAt is not null))
            .ToList();
    }
}

/// <summary>H3: members with access and server admins (D8); everybody else gets 404 (Q3).</summary>
public sealed record GetHouseholdQuery(Guid HouseholdId) : IQuery<HouseholdView>;

internal sealed class GetHouseholdHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : IQueryHandler<GetHouseholdQuery, HouseholdView>
{
    public async Task<Result<HouseholdView>> Handle(GetHouseholdQuery query, CancellationToken cancellationToken)
    {
        var found = await households.FindAsync(query.HouseholdId, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult<HouseholdView>();
        }

        var household = found.Value;
        var actor = currentUser.Actor();
        var now = time.GetUtcNow();
        if (!household.CanRead(actor, now))
        {
            return Result.Fail<HouseholdView>(HouseholdErrors.NotFound);
        }

        var role = household.RoleOf(actor, now);
        HouseholdRole? myRole = role.IsSuccess ? role.Value : null;
        return new HouseholdView(
            household.Id,
            household.Name.Value,
            household.Description?.Value,
            household.Status,
            household.CreatedAt,
            household.ArchivedAt,
            myRole,
            household.CanSeeMembers(actor, now) ? household.Members.Count : null,
            myRole >= HouseholdRole.Admin ? household.GuestCode?.Display : null,
            myRole == HouseholdRole.Owner ? household.OwnershipReceivedAt : null);
    }
}

/// <summary>H4: the whole form, name and description.</summary>
public sealed record UpdateHouseholdCommand(Guid HouseholdId, string Name, string? Description) : ICommand;

internal sealed class UpdateHouseholdValidator : AbstractValidator<UpdateHouseholdCommand>
{
    public UpdateHouseholdValidator() => RuleFor(c => c.Name).NotNull();
}

internal sealed class UpdateHouseholdHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UpdateHouseholdCommand>
{
    public async Task<Result> Handle(UpdateHouseholdCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed
            ? household.ToResult()
            : household.Value.Update(currentUser.Actor(), command.Name, command.Description, time);
    }
}

/// <summary>H5, D7: "delete household" for the Owner, archive for a server admin. Its invites are revoked (Events).</summary>
public sealed record ArchiveHouseholdCommand(Guid HouseholdId) : ICommand;

internal sealed class ArchiveHouseholdHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<ArchiveHouseholdCommand>
{
    public async Task<Result> Handle(ArchiveHouseholdCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed ? household.ToResult() : household.Value.Archive(currentUser.Actor(), time);
    }
}

/// <summary>H6, D7: only a server admin.</summary>
public sealed record RestoreHouseholdCommand(Guid HouseholdId) : ICommand;

internal sealed class RestoreHouseholdHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RestoreHouseholdCommand>
{
    public async Task<Result> Handle(RestoreHouseholdCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed ? household.ToResult() : household.Value.Restore(currentUser.Actor(), time);
    }
}

/// <summary>D5a: the new Owner closes the "you own this household now" banner.</summary>
public sealed record AcknowledgeOwnershipCommand(Guid HouseholdId) : ICommand;

internal sealed class AcknowledgeOwnershipHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<AcknowledgeOwnershipCommand>
{
    public async Task<Result> Handle(AcknowledgeOwnershipCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed ? household.ToResult() : household.Value.AcknowledgeOwnership(currentUser.Actor(), time);
    }
}

/// <summary>D2b: a new guest link (the old one stops working); returns the code as people see it.</summary>
public sealed record RegenerateGuestLinkCommand(Guid HouseholdId) : ICommand<string>;

internal sealed class RegenerateGuestLinkHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RegenerateGuestLinkCommand, string>
{
    public async Task<Result<string>> Handle(RegenerateGuestLinkCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        if (household.IsFailed)
        {
            return household.ToResult<string>();
        }

        var code = household.Value.RegenerateGuestLink(currentUser.Actor(), time);
        return code.IsFailed ? code.ToResult<string>() : code.Value.Display;
    }
}

/// <summary>D2b: turns the guest link off.</summary>
public sealed record DisableGuestLinkCommand(Guid HouseholdId) : ICommand;

internal sealed class DisableGuestLinkHandler(IHouseholdRepository households, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<DisableGuestLinkCommand>
{
    public async Task<Result> Handle(DisableGuestLinkCommand command, CancellationToken cancellationToken)
    {
        var household = await households.FindAsync(command.HouseholdId, cancellationToken);
        return household.IsFailed ? household.ToResult() : household.Value.DisableGuestLink(currentUser.Actor(), time);
    }
}

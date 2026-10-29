using Households.Domain.Common;
using Households.Domain.Invites;
using Households.Domain.Households.Events;
using FluentResults;
using HomeApps.BuildingBlocks.Domain;

namespace Households.Domain.Households;

/// <summary>
/// A family or group sharing the smart home (household.md 2): its name, status, guest link and members with their
/// household roles. Invariants: exactly one Owner while active, a person at most once, an archived household is read-only
/// and hidden from its members (D7), an Admin manages only lower roles.
/// </summary>
public sealed partial class Household : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<Membership> _members = [];

    private Household() { }

    private Household(Guid id, HouseholdName name, HouseholdDescription? description, DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Description = description;
        Status = HouseholdStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public HouseholdName Name { get; private set; } = null!;

    public HouseholdDescription? Description { get; private set; }

    public HouseholdStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>D2b: the one permanent guest link; null while the link is off.</summary>
    public InviteCode? GuestCode { get; private set; }

    /// <summary>D5a: set when ownership moved automatically; the new Owner sees a banner until they acknowledge it.</summary>
    public DateTimeOffset? OwnershipReceivedAt { get; private set; }

    public IReadOnlyList<Membership> Members => _members;

    /// <summary>Null only for an archived household whose Owner was erased with nobody to take over (D5).</summary>
    public Membership? Owner => _members.FirstOrDefault(m => m.Role == HouseholdRole.Owner);

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>H1: the creator becomes the only Owner. Whether the account is active is checked by the handler (Q5).</summary>
    public static Result<Household> Create(Guid id, Guid ownerId, string? name, string? description, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        if (id == Guid.Empty || ownerId == Guid.Empty)
        {
            throw new ArgumentException("Household and owner ids cannot be empty.");
        }

        var validName = HouseholdName.Create(name);
        var validDescription = HouseholdDescription.Create(description);
        var check = Result.Merge(validName, validDescription);
        if (check.IsFailed)
        {
            return check.ToResult<Household>();
        }

        var now = time.GetUtcNow();
        var household = new Household(id, validName.Value, validDescription.Value, now);
        household._members.Add(Membership.Create(id, ownerId, HouseholdRole.Owner, null, null, now));
        household._domainEvents.Add(new HouseholdCreated(id, ownerId, validName.Value.Value));
        return household;
    }

    public Membership? MemberOf(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId);

    /// <summary>H3: members with access while the household is active (D7), and server admins always (D8).</summary>
    public bool CanRead(Actor actor, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.IsServerAdmin || RoleOf(actor, now).IsSuccess;
    }

    /// <summary>M1: the member list is private (D2a): not for guests; server admins see it (D8).</summary>
    public bool CanSeeMembers(Actor actor, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.IsServerAdmin || RoleOf(actor, now) is { IsSuccess: true, Value: >= HouseholdRole.Member };
    }

    /// <summary>The caller's role here, or <c>household.not_found</c> when they have no access (Q3, D2c, D7).</summary>
    public Result<HouseholdRole> RoleOf(Actor actor, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var membership = Acting(actor, now, HouseholdRole.Guest);
        return membership.IsSuccess ? membership.Value.Role : membership.ToResult<HouseholdRole>();
    }

    /// <summary>
    /// The caller's membership when they may act here with at least <paramref name="minimum"/>: no access → not found
    /// (Q3), lower role → forbidden.
    /// </summary>
    private Result<Membership> Acting(Actor actor, DateTimeOffset now, HouseholdRole minimum)
    {
        var membership = MemberOf(actor.Id);
        if (Status == HouseholdStatus.Archived || membership is null || !membership.HasAccess(now))
        {
            return Result.Fail<Membership>(HouseholdErrors.NotFound);
        }

        return membership.Role >= minimum ? membership : Result.Fail<Membership>(HouseholdErrors.Forbidden);
    }

    private void Touch(DateTimeOffset now) => UpdatedAt = now;
}

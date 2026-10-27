using Households.Application.Abstractions;
using Households.Domain.Households;
using HomeApps.BuildingBlocks.Application.Messaging;
using DomainEvents = Households.Domain.Households.Events;

namespace Households.Application.Events;

/// <summary>
/// Invites follow their household and their author, in the same transaction as the change:
/// archiving revokes all (D7), leaving, being removed or a guest's access ending revokes the person's own (D3c).
/// An erased author's invites keep working (Q6).
/// </summary>
internal sealed class InviteHousekeeping(IInviteRepository invites, TimeProvider time) :
    IDomainEventHandler<DomainEvents.HouseholdArchived>,
    IDomainEventHandler<DomainEvents.MemberLeft>
{
    public Task Handle(DomainEventNotification<DomainEvents.HouseholdArchived> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return RevokeAsync(notification.DomainEvent.HouseholdId, null, cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.MemberLeft> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return e.Reason == MemberLeftReason.Erased ? Task.CompletedTask : RevokeAsync(e.HouseholdId, e.UserId, cancellationToken);
    }

    private async Task RevokeAsync(Guid householdId, Guid? createdBy, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        foreach (var invite in await invites.ListNotRevokedAsync(householdId, createdBy, cancellationToken))
        {
            invite.Revoke(now);
        }
    }
}

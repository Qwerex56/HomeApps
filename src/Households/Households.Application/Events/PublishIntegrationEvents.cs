using Households.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Contracts = HomeApps.Contracts.Households;
using DomainEvents = Households.Domain.Households.Events;

namespace Households.Application.Events;

/// <summary>
/// Turns the Household aggregate's domain events into <c>HomeApps.Contracts.Households</c> messages (D9). Runs before the
/// commit, so each message lands in the outbox in the same transaction as the change.
/// </summary>
internal sealed class PublishIntegrationEvents(IEventBus bus, IHouseholdRepository households) :
    IDomainEventHandler<DomainEvents.HouseholdCreated>,
    IDomainEventHandler<DomainEvents.HouseholdUpdated>,
    IDomainEventHandler<DomainEvents.HouseholdArchived>,
    IDomainEventHandler<DomainEvents.HouseholdRestored>,
    IDomainEventHandler<DomainEvents.MemberJoined>,
    IDomainEventHandler<DomainEvents.MemberLeft>,
    IDomainEventHandler<DomainEvents.MemberRoleChanged>,
    IDomainEventHandler<DomainEvents.OwnershipTransferred>
{
    public Task Handle(DomainEventNotification<DomainEvents.HouseholdCreated> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.HouseholdCreated(e.HouseholdId, e.OwnerId, e.Name), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.HouseholdUpdated> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.HouseholdUpdated(e.HouseholdId, e.Name, e.Description), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.HouseholdArchived> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return bus.PublishAsync(new Contracts.HouseholdArchived(notification.DomainEvent.HouseholdId), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.HouseholdRestored> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        return bus.PublishAsync(new Contracts.HouseholdRestored(notification.DomainEvent.HouseholdId), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.MemberJoined> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.MemberJoined(e.HouseholdId, e.UserId, e.Role.ToContract()), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.MemberLeft> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return bus.PublishAsync(
            new Contracts.MemberLeft(e.HouseholdId, e.UserId, (Contracts.MemberLeftReason)(int)e.Reason), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.MemberRoleChanged> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return bus.PublishAsync(
            new Contracts.MemberRoleChanged(e.HouseholdId, e.UserId, e.OldRole.ToContract(), e.NewRole.ToContract()), cancellationToken);
    }

    public async Task Handle(DomainEventNotification<DomainEvents.OwnershipTransferred> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        var household = await households.GetByIdAsync(e.HouseholdId, cancellationToken);
        await bus.PublishAsync(
            new Contracts.OwnershipTransferred(
                e.HouseholdId, household?.Name.Value ?? string.Empty, e.FromUserId, e.ToUserId, (Contracts.OwnershipTransferReason)(int)e.Reason),
            cancellationToken);
    }
}

internal static class ContractMapping
{
    public static Contracts.HouseholdRole ToContract(this Domain.Households.HouseholdRole role) => (Contracts.HouseholdRole)(int)role;
}

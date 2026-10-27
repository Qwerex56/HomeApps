using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Contracts = HomeApps.Contracts.Accounts;
using DomainEvents = Accounts.Domain.Users.Events;

namespace Accounts.Application.Events;

/// <summary>
/// Turns the domain events other services care about into <c>HomeApps.Contracts</c> messages. Runs before the commit,
/// so the message lands in the outbox in the same transaction as the change.
/// </summary>
internal sealed class PublishIntegrationEvents(IEventBus bus) :
    IDomainEventHandler<DomainEvents.UserRegistered>,
    IDomainEventHandler<DomainEvents.UserProfileUpdated>,
    IDomainEventHandler<DomainEvents.UserDisabled>,
    IDomainEventHandler<DomainEvents.UserEnabled>,
    IDomainEventHandler<DomainEvents.UserErased>
{
    public Task Handle(DomainEventNotification<DomainEvents.UserRegistered> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.UserCreated(e.UserId, e.DisplayName, e.AvatarUrl?.ToString()), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.UserProfileUpdated> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.UserProfileUpdated(e.UserId, e.DisplayName, e.AvatarUrl?.ToString()), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.UserDisabled> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.UserDisabled(e.UserId, e.BySelf), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.UserEnabled> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return bus.PublishAsync(new Contracts.UserEnabled(notification.DomainEvent.UserId), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.UserErased> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        return bus.PublishAsync(new Contracts.UserErased(notification.DomainEvent.UserId), cancellationToken);
    }
}

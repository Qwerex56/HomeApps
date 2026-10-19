using Auth.Application.Common;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Contracts = HomeApps.Contracts.Auth;
using DomainEvents = Auth.Domain.Identities.Events;

namespace Auth.Application.Events;

/// <summary>
/// Turns the domain events other services care about into <c>HomeApps.Contracts</c> messages. Runs before the commit,
/// so the message lands in the outbox in the same transaction as the change.
/// </summary>
internal sealed class PublishIntegrationEvents(IEventBus bus) :
    IDomainEventHandler<DomainEvents.IdentityRegistered>,
    IDomainEventHandler<DomainEvents.IdentitySignedIn>,
    IDomainEventHandler<DomainEvents.IdentityEmailChanged>,
    IDomainEventHandler<DomainEvents.IdentityRoleChanged>
{
    public Task Handle(DomainEventNotification<DomainEvents.IdentityRegistered> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        return bus.PublishAsync(
            new Contracts.IdentityRegistered(
                e.IdentityId,
                e.Email.Value,
                e.Role.ToContract(),
                e.Profile.DisplayName,
                e.Profile.FirstName,
                e.Profile.LastName,
                e.Profile.AvatarUrl?.ToString(),
                e.Source.ToContract(),
                e.RegisteredAt,
                ViaReferral: e.InvitedByEmail is not null,
                e.InvitedByEmail?.Value,
                e.InvitedEmail?.Value,
                new Contracts.RegistrationContext(e.Context.ClientApp, e.Context.UserAgent, e.Context.Locale)),
            cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.IdentitySignedIn> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.IdentitySignedIn(e.IdentityId, e.At, e.Method.ToContract(), e.ClientId), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.IdentityEmailChanged> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.IdentityEmailChanged(e.IdentityId, e.Email.Value), cancellationToken);
    }

    public Task Handle(DomainEventNotification<DomainEvents.IdentityRoleChanged> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        return bus.PublishAsync(new Contracts.IdentityRoleChanged(e.IdentityId, e.Role.ToContract()), cancellationToken);
    }
}

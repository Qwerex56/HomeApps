using Auth.Domain.Identities.Events;
using Auth.Domain.Invitations;
using Auth.Domain.Invitations.Events;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Events;

/// <summary>Every domain event that needs an email becomes one <see cref="AuthEmailRequested"/> in the outbox (Q6).</summary>
internal sealed class RequestEmails(IEventBus bus) :
    IDomainEventHandler<InvitationCreated>,
    IDomainEventHandler<PasswordResetRequested>,
    IDomainEventHandler<PasswordChangeRequested>,
    IDomainEventHandler<EmailChangeRequested>,
    IDomainEventHandler<AccountReactivatedBySelf>
{
    public Task Handle(DomainEventNotification<InvitationCreated> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var e = notification.DomainEvent;
        var template = e.Kind == InvitationKind.Bootstrap ? AuthEmailTemplate.BootstrapInvitation : AuthEmailTemplate.Invitation;
        return Send(new AuthEmailRequested(template, e.Email.Value, e.RawToken, e.Code.Value, e.InvitedByEmail?.Value, e.ExpiresAt), cancellationToken);
    }

    public Task Handle(DomainEventNotification<PasswordResetRequested> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return Send(new AuthEmailRequested(AuthEmailTemplate.PasswordReset, e.Email.Value, e.RawToken), cancellationToken);
    }

    public Task Handle(DomainEventNotification<PasswordChangeRequested> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return Send(new AuthEmailRequested(AuthEmailTemplate.PasswordChange, e.Email.Value, e.RawToken), cancellationToken);
    }

    public Task Handle(DomainEventNotification<EmailChangeRequested> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return Send(new AuthEmailRequested(AuthEmailTemplate.EmailChange, e.NewEmail.Value, e.RawToken), cancellationToken);
    }

    public Task Handle(DomainEventNotification<AccountReactivatedBySelf> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var e = notification.DomainEvent;
        return Send(new AuthEmailRequested(AuthEmailTemplate.AccountReactivated, e.Email.Value, null), cancellationToken);
    }

    private Task Send(AuthEmailRequested email, CancellationToken cancellationToken) => bus.PublishAsync(email, cancellationToken);
}

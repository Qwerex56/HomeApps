using Auth.Application.Events;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using Auth.Domain.Invitations;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.BuildingBlocks.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Contracts = HomeApps.Contracts.Auth;
using DomainEvents = Auth.Domain.Identities.Events;

namespace Auth.Application.Tests;

public sealed class EventTests
{
    private readonly World _w = new();

    /// <summary>Hands every event to the handlers the way the unit of work does before committing.</summary>
    private async Task Dispatch(IEnumerable<IDomainEvent> events)
    {
        var handlers = new object[]
        {
            new PublishIntegrationEvents(_w.Bus), new RequestEmails(_w.Bus), new LogSecurityEvents(NullLogger<LogSecurityEvents>.Instance),
        };
        foreach (var domainEvent in events.ToList())
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = Activator.CreateInstance(notificationType, domainEvent)!;
            var handlerType = typeof(MediatR.INotificationHandler<>).MakeGenericType(notificationType);
            foreach (var handler in handlers.Where(handlerType.IsInstanceOfType))
            {
                await (Task)handlerType.GetMethod("Handle")!.Invoke(handler, [notification, CancellationToken.None])!;
            }
        }
    }

    [Fact]
    public async Task Invited_registration_publishes_the_contract_with_referral_and_a_sign_in()
    {
        var (adam, _) = await _w.Registered();
        var invitation = Invitation.Create(new Inviter(adam.Id, adam.Email, true), Email.Create("ola@example.com").Value, false, false, _w.Time).Value;
        var ola = Identity.AcceptInvitationWithPassword(
            invitation.Invitation, World.Password, RegistrationProfile.Create("Ola", "Ola", null, null).Value,
            new RegistrationContext("homeapps-web", "Firefox", "pl-PL"), ClientInfo.Create("homeapps-web", null).Value,
            false, _w.Hasher, _w.Time).Value.Identity;

        await Dispatch(ola.DomainEvents);

        var registered = Assert.IsType<Contracts.IdentityRegistered>(Assert.Single(_w.Bus.Published));
        Assert.Equal(ola.Id, registered.UserId);
        Assert.True(registered.ViaReferral);
        Assert.Equal("adam@example.com", registered.InvitedByEmail);
        Assert.Equal("ola@example.com", registered.InvitedEmail);
        Assert.Equal(Contracts.ServerRole.User, registered.Role);
        Assert.Equal("pl-PL", registered.Context.Locale);
    }

    [Fact]
    public async Task Bootstrap_registration_is_not_a_referral_and_carries_owner()
    {
        var bootstrap = Invitation.CreateBootstrap(Email.Create("owner@example.com").Value, _w.Time);
        var owner = Identity.AcceptInvitationWithPassword(
            bootstrap.Invitation, World.Password, RegistrationProfile.Create("Owner", null, null, null).Value,
            new RegistrationContext(null, null, null), ClientInfo.Create("homeapps-web", null).Value, false, _w.Hasher, _w.Time).Value.Identity;

        await Dispatch(owner.DomainEvents);

        var registered = Assert.IsType<Contracts.IdentityRegistered>(Assert.Single(_w.Bus.Published));
        Assert.False(registered.ViaReferral);
        Assert.Equal(Contracts.ServerRole.Owner, registered.Role);
        Assert.Equal(Contracts.RegistrationSource.Bootstrap, registered.Source);
    }

    [Fact]
    public async Task Refresh_is_published_as_a_refresh_sign_in()
    {
        await Dispatch([new DomainEvents.IdentitySignedIn(Guid.NewGuid(), null, "jarvis", _w.Time.Now)]);

        var signedIn = Assert.IsType<Contracts.IdentitySignedIn>(Assert.Single(_w.Bus.Published));
        Assert.Equal(Contracts.SignInMethod.Refresh, signedIn.Method);
        Assert.Equal("jarvis", signedIn.ClientApp);
    }

    [Fact]
    public async Task Emails_become_one_internal_message_with_the_template()
    {
        var bootstrap = Invitation.CreateBootstrap(Email.Create("owner@example.com").Value, _w.Time);

        await Dispatch(
        [
            .. bootstrap.Invitation.DomainEvents,
            new DomainEvents.PasswordResetRequested(Guid.NewGuid(), Email.Create("a@example.com").Value, "raw"),
            new DomainEvents.AccountReactivatedBySelf(Guid.NewGuid(), Email.Create("a@example.com").Value),
        ]);

        var emails = _w.Bus.Published.Cast<AuthEmailRequested>().ToList();
        Assert.Equal(
            [AuthEmailTemplate.BootstrapInvitation, AuthEmailTemplate.PasswordReset, AuthEmailTemplate.AccountReactivated],
            emails.Select(e => e.Template));
        Assert.Equal(bootstrap.RawToken, emails[0].Token);
        Assert.Equal(bootstrap.Invitation.Code.Value, emails[0].InvitationCode);
        Assert.Null(emails[2].Token);
    }

    [Fact]
    public async Task Refresh_token_reuse_is_only_logged()
    {
        await Dispatch([new DomainEvents.RefreshTokenReuseDetected(Guid.NewGuid(), Guid.NewGuid(), "jarvis")]);

        Assert.Empty(_w.Bus.Published);
    }

    [Fact]
    public void Every_domain_event_has_a_handler()
    {
        var handled = typeof(PublishIntegrationEvents).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToHashSet();

        var events = typeof(Identity).Assembly.GetTypes().Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && !t.IsAbstract);

        Assert.All(events, e => Assert.Contains(e, handled));
    }
}

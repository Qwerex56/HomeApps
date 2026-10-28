using Auth.Application.AccountsSync;
using Auth.Application.Events;
using Auth.Domain.Identities;
using Auth.Infrastructure.Accounts;
using Auth.Infrastructure.Emails;
using FluentResults;
using HomeApps.Contracts.Accounts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Auth.Infrastructure.Tests;

public sealed class MessagingTests
{
    private static readonly Uri Frontend = new("https://home.example.com/");

    private static ConsumeContext<T> Context<T>(T message)
        where T : class
    {
        var context = Substitute.For<ConsumeContext<T>>();
        context.Message.Returns(message);
        return context;
    }

    [Theory]
    [InlineData(AuthEmailTemplate.Invitation, "https://home.example.com/invite/abc")]
    [InlineData(AuthEmailTemplate.BootstrapInvitation, "https://home.example.com/invite/abc")]
    [InlineData(AuthEmailTemplate.PasswordReset, "https://home.example.com/reset-password?token=abc")]
    [InlineData(AuthEmailTemplate.PasswordChange, "https://home.example.com/confirm-password?token=abc")]
    [InlineData(AuthEmailTemplate.EmailChange, "https://home.example.com/confirm-email?token=abc")]
    public void Every_email_with_a_token_links_into_the_app(AuthEmailTemplate template, string expected)
    {
        var message = EmailTemplates.Render(new AuthEmailRequested(template, "a@example.com", "abc"), Frontend);

        Assert.Contains(expected, message.Text, StringComparison.Ordinal);
        Assert.Contains($"href=\"{expected.Replace("&", "&amp;", StringComparison.Ordinal)}\"", message.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void Invitation_names_the_inviter_and_code_and_encodes_html()
    {
        var message = EmailTemplates.Render(
            new AuthEmailRequested(AuthEmailTemplate.Invitation, "ola@example.com", "abc", "K7P-2QX", "<adam>@example.com"), Frontend);

        Assert.Equal("Zaproszenie do HomeApps", message.Subject);
        Assert.Contains("K7P-2QX", message.Text, StringComparison.Ordinal);
        Assert.Contains("&lt;adam&gt;@example.com", message.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void Reactivation_notice_has_no_link()
    {
        var message = EmailTemplates.Render(new AuthEmailRequested(AuthEmailTemplate.AccountReactivated, "a@example.com", null), Frontend);

        Assert.DoesNotContain("http", message.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false, "stracił dostęp")]
    [InlineData(true, "zostało usunięte")]
    public void Household_ownership_mail_names_the_household_and_links_to_it(bool erased, string why)
    {
        var household = Guid.NewGuid();
        var message = EmailTemplates.Render(new AuthEmailRequested(
            AuthEmailTemplate.HouseholdOwnershipReceived, "ewa@example.com", null,
            HouseholdId: household, HouseholdName: "<Kowalscy>", PreviousOwnerErased: erased), Frontend);

        Assert.Equal("Przejęcie domu „<Kowalscy>” w HomeApps", message.Subject);
        Assert.Contains(why, message.Text, StringComparison.Ordinal);
        Assert.Contains($"https://home.example.com/households/{household}", message.Text, StringComparison.Ordinal);
        Assert.Contains("&lt;Kowalscy&gt;", message.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ownership_transferred_sends_its_command()
    {
        var sender = Substitute.For<ISender>();
        var message = new HomeApps.Contracts.Households.OwnershipTransferred(
            Guid.NewGuid(), "Dom", Guid.NewGuid(), Guid.NewGuid(), HomeApps.Contracts.Households.OwnershipTransferReason.OwnerBanned);

        await new Households.OwnershipTransferredConsumer(sender).Consume(Context(message));

        await sender.Received(1).Send(
            new Application.HouseholdsSync.NotifyOwnershipReceivedCommand(message.HouseholdId, "Dom", message.ToUserId, message.Reason),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_consumer_renders_and_sends()
    {
        var sender = new RecordingSender();
        var consumer = new AuthEmailConsumer(
            sender, Options.Create(new EmailOptions { FrontendBaseUrl = Frontend }), NullLogger<AuthEmailConsumer>.Instance);

        await consumer.Consume(Context(new AuthEmailRequested(AuthEmailTemplate.BootstrapInvitation, "owner@example.com", "abc")));

        var sent = Assert.Single(sender.Sent);
        Assert.Equal("owner@example.com", sent.To);
        Assert.Contains("/invite/abc", sent.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true, AccountStatus.DisabledBySelf)]
    [InlineData(false, AccountStatus.DisabledByAdmin)]
    public async Task User_disabled_maps_who_disabled_it(bool bySelf, AccountStatus expected)
    {
        var sender = Substitute.For<ISender>();
        var userId = Guid.NewGuid();

        await new UserDisabledConsumer(sender).Consume(Context(new UserDisabled(userId, bySelf)));

        await sender.Received(1).Send(new ApplyAccountStatusCommand(userId, expected), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task User_enabled_and_erased_send_their_commands()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result>>(), Arg.Any<CancellationToken>()).Returns(Result.Ok());
        var userId = Guid.NewGuid();

        await new UserEnabledConsumer(sender).Consume(Context(new UserEnabled(userId)));
        await new UserErasedConsumer(sender).Consume(Context(new UserErased(userId)));

        await sender.Received(1).Send(new ApplyAccountStatusCommand(userId, AccountStatus.Active), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new EraseIdentityCommand(userId), Arg.Any<CancellationToken>());
    }

    private sealed class RecordingSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }
}

using Auth.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Emails;

/// <summary>A-34: sends the email the outbox delivered. A failing SMTP server is retried by the definition below.</summary>
public sealed partial class AuthEmailConsumer(IEmailSender sender, IOptions<EmailOptions> options, ILogger<AuthEmailConsumer> logger)
    : IConsumer<AuthEmailRequested>
{
    public async Task Consume(ConsumeContext<AuthEmailRequested> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var email = context.Message;
        var frontend = options.Value.FrontendBaseUrl!;
        if (email.Template == AuthEmailTemplate.BootstrapInvitation)
        {
            // A-30: the server may have no working mail yet, so the owner's link is always in the log too.
            LogBootstrapLink(logger, email.To, EmailTemplates.Link(email, frontend));
        }

        await sender.SendAsync(EmailTemplates.Render(email, frontend), context.CancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Bootstrap invitation for the Owner {Email}: {Link}")]
    private static partial void LogBootstrapLink(ILogger logger, string email, Uri? link);
}

public sealed class AuthEmailConsumerDefinition : ConsumerDefinition<AuthEmailConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AuthEmailConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(endpointConfigurator);
        endpointConfigurator.UseMessageRetry(retry => retry.Intervals(
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)));
    }
}

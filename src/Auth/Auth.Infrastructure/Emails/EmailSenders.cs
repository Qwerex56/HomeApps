using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure.Emails;

public sealed record EmailMessage(string To, string Subject, string Text, string Html);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>Plain SMTP through MailKit; the provider (relay) is only configuration.</summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.From));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { TextBody = message.Text, HtmlBody = message.Html }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Smtp.Host!, settings.Smtp.Port, SecureSocketOptions.Auto, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Smtp.Username))
        {
            await client.AuthenticateAsync(settings.Smtp.Username, settings.Smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}

/// <summary>Development without SMTP (Q11): the email, link included, goes to the log instead.</summary>
internal sealed partial class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        LogEmail(logger, message.To, message.Subject, message.Text);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SMTP is not configured. Email to {To}, \"{Subject}\":\n{Text}")]
    private static partial void LogEmail(ILogger logger, string to, string subject, string text);
}

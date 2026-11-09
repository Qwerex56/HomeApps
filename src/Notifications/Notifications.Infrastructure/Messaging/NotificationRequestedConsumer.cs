using HomeApps.Contracts.Notifications;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Notifications.Application.Delivery.Deliver;

namespace Notifications.Infrastructure.Messaging;

/// <summary>
/// Delivers notifications other services ask for. A request that breaks the rules (no recipients, a full URL) is a bug
/// in the sender: it is logged and dropped rather than retried. Public: MassTransit only finds public consumers.
/// </summary>
public sealed partial class NotificationRequestedConsumer(ISender sender, ILogger<NotificationRequestedConsumer> logger)
    : IConsumer<NotificationRequested>
{
    public async Task Consume(ConsumeContext<NotificationRequested> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        var result = await sender.Send(new DeliverNotificationCommand(message), context.CancellationToken);
        if (result.IsFailed)
        {
            LogDropped(logger, message.Source, message.Category, string.Join("; ", result.Errors.Select(e => e.Message)));
            return;
        }

        var report = result.Value;
        LogDelivered(logger, message.Source, message.Category, report.Delivered, report.Skipped, report.Gone, report.Failed);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dropped a notification {Source}/{Category}: {Errors}")]
    private static partial void LogDropped(ILogger logger, string source, string category, string errors);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Notification {Source}/{Category}: {Delivered} devices, {Skipped} people skipped, {Gone} gone, {Failed} failed")]
    private static partial void LogDelivered(ILogger logger, string source, string category, int delivered, int skipped, int gone, int failed);
}

using HomeApps.Contracts.Notifications;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Delivery;

/// <summary>
/// Sends one notification to every device of one person and keeps the device list honest: a subscription the push
/// service calls gone is removed, a reached one is marked used. Shared by the delivery and the test command.
/// </summary>
internal sealed partial class NotificationDispatcher(IPushSender sender, TimeProvider time, ILogger<NotificationDispatcher> logger)
{
    /// <summary>Shown instead of the content to people who hide it on the lock screen (N4), when the sender gave nothing.</summary>
    public const string DefaultHiddenBody = "Nowe powiadomienie";

    /// <summary>Web Push carries at most ~4 KB; a notification shows a few lines anyway.</summary>
    public const int MaxTitleLength = 100;

    public const int MaxBodyLength = 300;

    public async Task<DeliveryReport> SendAsync(Recipient recipient, NotificationRequested request, CancellationToken cancellationToken)
    {
        var body = recipient.ShowPreview ? request.Body : request.BodyWhenHidden ?? DefaultHiddenBody;
        var message = new PushMessage(
            Shorten(request.Title, MaxTitleLength),
            Shorten(body, MaxBodyLength),
            request.Url,
            request.Source,
            request.Category,
            request.HouseholdId,
            request.Tag,
            request.Urgency);

        int delivered = 0, gone = 0, failed = 0;
        foreach (var device in recipient.Devices.ToList())
        {
            switch (await sender.SendAsync(device.Target, message, cancellationToken))
            {
                case PushOutcome.Delivered:
                    recipient.DeviceReached(device.Id, time.GetUtcNow());
                    delivered++;
                    break;
                case PushOutcome.Gone:
                    recipient.DeviceGone(device.Id);
                    LogGone(logger, device.Id, recipient.UserId);
                    gone++;
                    break;
                default:
                    failed++;
                    break;
            }
        }

        return new DeliveryReport(delivered, gone, failed, 0);
    }

    private static string Shorten(string text, int max) => text.Length <= max ? text : string.Concat(text.AsSpan(0, max - 1), "…");

    [LoggerMessage(Level = LogLevel.Information, Message = "Push subscription {DeviceId} of {UserId} is gone; device removed")]
    private static partial void LogGone(ILogger logger, Guid deviceId, Guid userId);
}

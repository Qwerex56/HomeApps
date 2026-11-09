using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomeApps.Contracts.Notifications;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;
using AppPushMessage = Notifications.Application.Abstractions.PushMessage;
using WebPushMessage = Lib.Net.Http.WebPush.PushMessage;

namespace Notifications.Infrastructure.Push;

/// <summary>
/// Encrypts the message for the browser (aes128gcm, RFC 8291), signs the request with the instance's VAPID key and
/// posts it to the browser's push service. The content is JSON for the service worker (docs/notifications.md).
/// </summary>
internal sealed partial class WebPushSender(PushServiceClient client, VapidKeys keys, ILogger<WebPushSender> logger) : IPushSender
{
    /// <summary>How long a push service keeps a message for a device that is offline.</summary>
    public static readonly TimeSpan TimeToLive = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PushOutcome> SendAsync(PushTarget target, AppPushMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(message);

        var subscription = new PushSubscription { Endpoint = target.Endpoint };
        subscription.SetKey(PushEncryptionKeyName.P256DH, target.P256dh);
        subscription.SetKey(PushEncryptionKeyName.Auth, target.Auth);

        var payload = JsonSerializer.Serialize(
            new
            {
                message.Title,
                message.Body,
                message.Url,
                message.Source,
                message.Category,
                message.HouseholdId,
                message.Tag,
            },
            Json);
        var push = new WebPushMessage(payload)
        {
            TimeToLive = (int)TimeToLive.TotalSeconds,
            Urgency = message.Urgency == NotificationUrgency.High ? PushMessageUrgency.High : PushMessageUrgency.Normal,
            Topic = Topic(message.Tag),
        };

        using var vapid = new VapidAuthentication(keys.PublicKey, keys.PrivateKey) { Subject = keys.Subject };
        try
        {
            await client.RequestPushMessageDeliveryAsync(subscription, push, vapid, cancellationToken);
            return PushOutcome.Delivered;
        }
        catch (PushServiceClientException exception) when (exception.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            return PushOutcome.Gone;
        }
        catch (PushServiceClientException exception)
        {
            LogRejected(logger, (int)exception.StatusCode, Host(target.Endpoint), exception.Body);
            return PushOutcome.Failed;
        }
        catch (HttpRequestException exception)
        {
            LogUnreachable(logger, Host(target.Endpoint), exception);
            return PushOutcome.Failed;
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogUnreachable(logger, Host(target.Endpoint), exception);
            return PushOutcome.Failed;
        }
    }

    /// <summary>
    /// The push service keeps only the newest undelivered message per topic, so ten chat messages to a phone that is
    /// off arrive as one. A topic is at most 32 base64url characters; longer tags are hashed.
    /// </summary>
    internal static string? Topic(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return null;
        }

        if (tag.Length <= 32 && tag.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            return tag;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tag));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_')[..32];
    }

    private static string Host(string endpoint) => Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : "?";

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push service {Host} refused a notification with {Status}: {Body}")]
    private static partial void LogRejected(ILogger logger, int status, string host, string? body);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Push service {Host} could not be reached")]
    private static partial void LogUnreachable(ILogger logger, string host, Exception exception);
}

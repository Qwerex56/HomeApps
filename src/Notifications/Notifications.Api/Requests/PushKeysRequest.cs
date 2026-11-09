namespace Notifications.Api.Requests;

internal sealed record PushKeysRequest(string P256dh, string Auth);

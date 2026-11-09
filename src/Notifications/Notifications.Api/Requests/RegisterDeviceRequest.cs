namespace Notifications.Api.Requests;

/// <summary>Exactly what <c>PushSubscription.toJSON()</c> gives in the browser.</summary>
internal sealed record RegisterDeviceRequest(string Endpoint, PushKeysRequest Keys);

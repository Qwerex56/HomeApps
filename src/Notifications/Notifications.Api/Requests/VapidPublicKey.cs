namespace Notifications.Api.Requests;

/// <param name="PublicKey">The <c>applicationServerKey</c> for <c>PushManager.subscribe()</c>, base64url.</param>
internal sealed record VapidPublicKey(string PublicKey);

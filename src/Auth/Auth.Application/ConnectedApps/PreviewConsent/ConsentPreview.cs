namespace Auth.Application.ConnectedApps.PreviewConsent;

/// <param name="ClientName">What the app calls itself (not verified).</param>
/// <param name="RedirectHost">Where the person will be sent back, so a fake name is easier to spot.</param>
public sealed record ConsentPreview(string ClientId, string ClientName, string RedirectHost, string Resource);

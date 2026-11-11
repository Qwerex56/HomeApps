using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.DecideConsent;

/// <summary>
/// The signed-in person allows or refuses the app (consent screen in Vue). Either way the answer is where to send the
/// browser: back to the app with a one-minute code, or with <c>error=access_denied</c>.
/// </summary>
public sealed record DecideConsentCommand(AuthorizationRequest Request, bool Allow) : ICommand<ConsentRedirect>;

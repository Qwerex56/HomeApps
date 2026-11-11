using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.ConnectedApps.PreviewConsent;

/// <summary>What the consent screen shows before the person decides; fails the same way the decision would.</summary>
public sealed record PreviewConsentQuery(AuthorizationRequest Request) : IQuery<ConsentPreview>;

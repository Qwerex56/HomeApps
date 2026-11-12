using HomeMix.Application.Installations;

namespace HomeMix.Api.Requests;

/// <param name="Consent">Only when the new version asks for more than was granted (409 <c>homemix.consent_required</c>).</param>
public sealed record UpdateInstallationRequest(string Sha256, ConsentInput? Consent);

using HomeMix.Application.Installations;

namespace HomeMix.Api.Requests;

/// <param name="Sha256">The package, as adding it answered.</param>
/// <param name="Consent">What the installer accepted on the consent screen.</param>
public sealed record InstallRequest(string Sha256, ConsentInput Consent);

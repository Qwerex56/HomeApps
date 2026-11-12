using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations;

/// <summary>The consent screen's answer as it arrives in a request.</summary>
public sealed record ConsentInput(IReadOnlyList<string>? Capabilities, IReadOnlyList<string>? Network, bool Jarvis)
{
    public Consent ToConsent() => new(Capabilities ?? [], Network ?? [], Jarvis);
}

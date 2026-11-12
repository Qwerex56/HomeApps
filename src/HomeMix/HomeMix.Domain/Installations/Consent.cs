namespace HomeMix.Domain.Installations;

/// <summary>What the installer accepted on the consent screen: capabilities, network origins and whether JARVIS may use it.</summary>
public sealed record Consent(IReadOnlyCollection<string> Capabilities, IReadOnlyCollection<string> Network, bool Jarvis);

namespace HomeMix.Application.Abstractions;

/// <summary>A downloaded <c>addon.zip</c> whose SHA-256 matched the digest GitHub gives for the release asset.</summary>
public sealed record GitHubRelease(string Repository, string Tag, byte[] Zip, string Sha256);

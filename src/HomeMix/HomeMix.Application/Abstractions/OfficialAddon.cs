using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Abstractions;

/// <summary>An official add-on shipped inside the HomeMix image (<c>homemix/official/*</c>, built with it).</summary>
public sealed record OfficialAddon(AddonManifest Manifest, string Sha256, long Size);

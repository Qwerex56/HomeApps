using HomeMix.Domain.Packages;

namespace HomeMix.Application.Abstractions;

/// <summary>What a zip holds: homemix.json as text and the list of files.</summary>
public sealed record PackageContents(string ManifestJson, IReadOnlyList<PackageFile> Files);

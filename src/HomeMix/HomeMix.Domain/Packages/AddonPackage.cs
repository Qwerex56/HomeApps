using FluentResults;
using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Packages;

/// <summary>
/// One version of an add-on as a checked zip, known by its SHA-256. The same package serves every installation of it,
/// so the server keeps one copy (W9). The zip itself lives in the package store; this is what HomeMix knows about it.
/// </summary>
public sealed class AddonPackage
{
    private AddonManifest? _manifest;

    private AddonPackage() { }

    /// <summary>Lowercase hex SHA-256 of the zip.</summary>
    public string Sha256 { get; private set; } = string.Empty;

    public string AddonId { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    /// <summary>homemix.json as it was in the zip, so later stages can read fields this one does not know.</summary>
    public string ManifestJson { get; private set; } = string.Empty;

    /// <summary>Where it came from: a GitHub repository URL, or <c>null</c> for an uploaded zip.</summary>
    public string? Repository { get; private set; }

    /// <summary>The release tag on GitHub, when it came from there.</summary>
    public string? Tag { get; private set; }

    public long Size { get; private set; }

    public Guid AddedBy { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    public AddonManifest Manifest => _manifest ??= AddonManifestParser.Parse(ManifestJson).Value;

    /// <summary>Checks the manifest and the files; every problem at once.</summary>
    public static Result<AddonPackage> Create(
        string sha256, string manifestJson, IReadOnlyList<PackageFile> files, long size, string? repository, string? tag,
        Guid addedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        ArgumentNullException.ThrowIfNull(files);

        var manifest = AddonManifestParser.Parse(manifestJson);
        if (manifest.IsFailed)
        {
            return manifest.ToResult();
        }

        var problems = PackageRules.Check(files, manifest.Value);
        if (problems.Count > 0)
        {
            return Result.Fail(problems.Select(PackageErrors.Invalid));
        }

        return new AddonPackage
        {
            Sha256 = sha256.ToLowerInvariant(),
            AddonId = manifest.Value.Id,
            Version = manifest.Value.Version,
            ManifestJson = manifestJson,
            Repository = repository,
            Tag = tag,
            Size = size,
            AddedBy = addedBy,
            AddedAt = now,
            _manifest = manifest.Value,
        };
    }
}

using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Packages;

/// <summary>What an add-on's zip may hold (docs/homemix.md, "The package"): static files only, no tricks with paths.</summary>
public static class PackageRules
{
    public const string ManifestFile = "homemix.json";
    public const int MaxFiles = 500;

    /// <summary>Unpacked, against zip bombs; the zip itself is limited by configuration (5 MB by default).</summary>
    public const long MaxUnpackedBytes = 25L * 1024 * 1024;

    public static IReadOnlySet<string> AllowedExtensions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ".html", ".js", ".mjs", ".css", ".json", ".svg", ".png", ".webp", ".woff2", ".md",
    };

    /// <summary>Every problem with the files of a package whose manifest is <paramref name="manifest"/>.</summary>
    public static IReadOnlyList<string> Check(IReadOnlyList<PackageFile> files, AddonManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(manifest);
        var problems = new List<string>();

        if (files.Count > MaxFiles)
        {
            problems.Add($"A package may hold at most {MaxFiles} files.");
        }

        if (files.Sum(f => f.Length) > MaxUnpackedBytes)
        {
            problems.Add($"Unpacked, a package may have at most {MaxUnpackedBytes / (1024 * 1024)} MB.");
        }

        foreach (var file in files)
        {
            if (!IsSafePath(file.Path))
            {
                problems.Add($"'{file.Path}' is not allowed: no absolute paths, '..', backslashes or hidden files.");
            }
            else if (!AllowedExtensions.Contains(Path.GetExtension(file.Path).ToLowerInvariant()))
            {
                problems.Add($"'{file.Path}' is not allowed: only {string.Join(", ", AllowedExtensions)} files.");
            }
        }

        var paths = files.Select(f => f.Path).ToHashSet(StringComparer.Ordinal);
        if (paths.Count != files.Count)
        {
            problems.Add("The package holds the same path twice.");
        }

        foreach (var entry in new[] { manifest.Entry }.Concat(manifest.Widgets.Select(w => w.Entry)).OfType<string>().Distinct(StringComparer.Ordinal))
        {
            if (!paths.Contains(entry))
            {
                problems.Add($"'{entry}' is named in homemix.json but is not in the package.");
            }
        }

        return problems;
    }

    public static bool IsSafePath(string path) =>
        !string.IsNullOrEmpty(path)
        && path.Length <= 200
        && !path.StartsWith('/')
        && !path.Contains('\\', StringComparison.Ordinal)
        && !path.Contains(':', StringComparison.Ordinal)
        && path.Split('/').All(segment => segment.Length > 0 && segment != ".." && segment != "." && !segment.StartsWith('.'));
}

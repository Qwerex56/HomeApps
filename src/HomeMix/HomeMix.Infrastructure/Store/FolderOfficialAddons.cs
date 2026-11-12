using System.Security.Cryptography;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Manifests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure.Store;

/// <summary>
/// Reads the official add-ons once, from the <c>*.zip</c> files in <c>HomeMix:OfficialPath</c> that the image's build
/// made from <c>homemix/official/*</c>. A zip that does not open or has no valid manifest is logged and left out.
/// </summary>
internal sealed partial class FolderOfficialAddons(IPackageReader reader, IOptions<HomeMixOptions> options, ILogger<FolderOfficialAddons> logger)
    : IOfficialAddons
{
    private readonly Lazy<IReadOnlyList<(OfficialAddon Addon, string Path)>> _addons = new(() => Load(reader, options.Value.OfficialPath, logger));

    public IReadOnlyList<OfficialAddon> List() => [.. _addons.Value.Select(a => a.Addon)];

    public async Task<byte[]?> ReadAsync(string addonId, CancellationToken cancellationToken)
    {
        var found = _addons.Value.FirstOrDefault(a => a.Addon.Manifest.Id == addonId);
        return found.Path is null ? null : await File.ReadAllBytesAsync(found.Path, cancellationToken);
    }

    private static List<(OfficialAddon, string)> Load(IPackageReader reader, string folder, ILogger logger)
    {
        var addons = new List<(OfficialAddon, string)>();
        if (!Directory.Exists(folder))
        {
            return addons;
        }

        foreach (var path in Directory.EnumerateFiles(folder, "*.zip").Order(StringComparer.Ordinal))
        {
            var zip = File.ReadAllBytes(path);
            var manifest = reader.Read(zip).Bind(contents => AddonManifestParser.Parse(contents.ManifestJson));
            if (manifest.IsFailed)
            {
                LogSkipped(logger, path, string.Join(" ", manifest.Errors.Select(e => e.Message)));
                continue;
            }

            addons.Add((new OfficialAddon(manifest.Value, Convert.ToHexStringLower(SHA256.HashData(zip)), zip.LongLength), path));
        }

        return addons;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Official add-on {Path} left out: {Problem}")]
    private static partial void LogSkipped(ILogger logger, string path, string problem);
}

using HomeMix.Application.Blocks;
using System.Security.Cryptography;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Packages;

/// <summary>
/// The one way a zip becomes a package, from an upload or from GitHub: switched on, small enough, a valid manifest
/// and files, an id that is not reserved or blocked, a HomeApps new enough. A zip that is already known is reused.
/// </summary>
public sealed class PackageIntake(
    IPackageReader reader,
    IPackageStore store,
    IPackageRepository packages,
    ICurrentUser currentUser,
    TimeProvider time,
    Blocklist blocklist,
    IOptions<HomeMixOptions> options)
{
    /// <summary>The version of this HomeApps (Directory.Build.props).</summary>
    public static string HomeAppsVersion { get; } = typeof(PackageIntake).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public async Task<Result<AddonPackage>> AddAsync(byte[] zip, string? repository, string? tag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(zip);
        var settings = options.Value;
        if (!settings.UploadsEnabled)
        {
            return Result.Fail(PackageErrors.UploadsDisabled);
        }

        if (zip.LongLength > settings.MaxPackageBytes)
        {
            return Result.Fail(PackageErrors.TooLarge(settings.MaxPackageBytes));
        }

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(zip));
        var known = await packages.FindAsync(sha256, cancellationToken);
        if (known is not null)
        {
            return await blocklist.IsBlockedAsync(known.AddonId, sha256, cancellationToken) ? Result.Fail(PackageErrors.Blocked) : Result.Ok(known);
        }

        var contents = reader.Read(zip);
        if (contents.IsFailed)
        {
            return contents.ToResult();
        }

        var package = AddonPackage.Create(
            sha256, contents.Value.ManifestJson, contents.Value.Files, zip.LongLength, repository, tag,
            currentUser.RequiredUserId, time.GetUtcNow());
        if (package.IsFailed)
        {
            return package;
        }

        var manifest = package.Value.Manifest;
        if (ReservedAddonIds.Core.Contains(manifest.Id) || settings.PluginIds.Contains(manifest.Id))
        {
            return Result.Fail(ManifestErrors.ReservedId(manifest.Id));
        }

        if (await blocklist.IsBlockedAsync(manifest.Id, sha256, cancellationToken))
        {
            return Result.Fail(PackageErrors.Blocked);
        }

        if (!AddonVersion.AtLeast(HomeAppsVersion, manifest.RequiresHomeApps))
        {
            return Result.Fail(ManifestErrors.NeedsNewerHomeApps(manifest.RequiresHomeApps, HomeAppsVersion));
        }

        await store.SaveAsync(sha256, zip, cancellationToken);
        packages.Add(package.Value);
        return package;
    }
}

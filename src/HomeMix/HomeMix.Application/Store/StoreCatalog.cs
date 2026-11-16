using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Packages;
using HomeMix.Domain.Packages;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store;

/// <summary>
/// Reads a repository's latest release for the store (homemix-2.md 2.1): what GitHub says, then the package checked
/// like any add-on (manifest, files, reserved ids, blocks). A release whose SHA-256 is already known is not downloaded
/// again, so the hourly refresh costs two GitHub calls per entry.
/// </summary>
public sealed class StoreCatalog(
    IGitHubRepositories repositories, IGitHubReleases releases, IPackageRepository packages, PackageIntake intake, TimeProvider time)
{
    public async Task<Result<(AddonPackage Package, StoreRelease Release, string Repository)>> ReadAsync(
        string repository, Guid uploadedBy, CancellationToken cancellationToken)
    {
        var latest = await repositories.LatestAsync(repository, cancellationToken);
        if (latest.Problem is { } problem || latest.Repository is null || latest.Tag is null || latest.Sha256 is null)
        {
            return Result.Fail(StoreErrors.Unusable(latest.Problem ?? "unavailable"));
        }

        var release = new StoreRelease(latest.Tag, latest.PublishedAt, latest.Readme);
        var known = await packages.FindAsync(latest.Sha256.ToLowerInvariant(), cancellationToken);
        if (known is not null)
        {
            return (known, release, latest.Repository);
        }

        var downloaded = await releases.DownloadAsync(latest.Repository, latest.Tag, cancellationToken);
        if (downloaded.IsFailed)
        {
            return downloaded.ToResult();
        }

        var package = await intake.AddAsync(downloaded.Value.Zip, latest.Repository, latest.Tag, cancellationToken, uploadedBy);
        return package.IsFailed ? package.ToResult() : (package.Value, release, latest.Repository);
    }

    /// <summary>Reads the entry's repository again; a problem is kept on the entry, never thrown.</summary>
    public async Task RefreshAsync(StoreListing listing, Guid by, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listing);
        var read = await ReadAsync(listing.Repository, by, cancellationToken);
        if (read.IsFailed)
        {
            listing.Fail(ProblemOf(read.Errors), time.GetUtcNow());
            return;
        }

        listing.Refresh(read.Value.Package, read.Value.Release, time.GetUtcNow());
    }

    /// <summary>The short code kept on an entry: GitHub's problem, or the package's error code.</summary>
    public static string ProblemOf(IEnumerable<IError> errors)
    {
        var metadata = errors.FirstOrDefault()?.Metadata;
        if (metadata?.TryGetValue(StoreErrors.ProblemKey, out var problem) == true && problem is string text)
        {
            return text;
        }

        return metadata?.TryGetValue(AppError.CodeKey, out var code) == true && code is string value ? value : "unavailable";
    }
}

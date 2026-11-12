using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Run.GetRunFile;

internal sealed class GetRunFileHandler(
    IInstallationRepository installations, IPackageRepository packages, IPackageStore store, IOptions<HomeMixOptions> options)
    : IQueryHandler<GetRunFileQuery, RunFile>
{
    public async Task<Result<RunFile>> Handle(GetRunFileQuery query, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(query.InstallationId, cancellationToken);
        if (installation is null || !installation.IsActive || options.Value.IsBlocked(installation.AddonId, installation.PackageSha256))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        var path = query.Path;
        if (string.IsNullOrEmpty(path))
        {
            var package = await packages.FindAsync(installation.PackageSha256, cancellationToken);
            path = package?.Manifest.Entry ?? string.Empty;
        }

        if (!PackageRules.IsSafePath(path) || string.Equals(path, PackageRules.ManifestFile, StringComparison.Ordinal))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        var content = await store.OpenFileAsync(installation.PackageSha256, path, cancellationToken);
        return content is null ? Result.Fail(InstallationErrors.NotFound) : new RunFile(content, path, installation.Network);
    }
}

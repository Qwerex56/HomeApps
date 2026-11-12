using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.GetInstallation;

internal sealed class GetInstallationHandler(IInstallationRepository installations, IPackageRepository packages, InstallationAccess access)
    : IQueryHandler<GetInstallationQuery, InstallationView>
{
    public async Task<Result<InstallationView>> Handle(GetInstallationQuery query, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(query.InstallationId, cancellationToken);
        if (installation is null || !installation.IsActive || !await access.CanUseAsync(installation.Target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        var package = await packages.FindAsync(installation.PackageSha256, cancellationToken);
        return package is null ? Result.Fail(InstallationErrors.NotFound) : InstallationView.From(installation, package);
    }
}

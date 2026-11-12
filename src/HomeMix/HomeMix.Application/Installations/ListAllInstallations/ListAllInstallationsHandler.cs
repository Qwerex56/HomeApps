using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;

namespace HomeMix.Application.Installations.ListAllInstallations;

internal sealed class ListAllInstallationsHandler(IInstallationRepository installations, IPackageRepository packages, Blocklist blocklist)
    : IQueryHandler<ListAllInstallationsQuery, IReadOnlyList<AdminInstallationView>>
{
    public async Task<Result<IReadOnlyList<AdminInstallationView>>> Handle(ListAllInstallationsQuery query, CancellationToken cancellationToken)
    {
        var all = await installations.ListAllActiveAsync(cancellationToken);
        var byHash = (await packages.FindManyAsync([.. all.Select(i => i.PackageSha256).Distinct()], cancellationToken))
            .ToDictionary(p => p.Sha256);

        var blocked = await blocklist.SnapshotAsync(cancellationToken);
        return all
            .Select(i => (Installation: i, Package: byHash[i.PackageSha256]))
            .Select(x => new AdminInstallationView(
                x.Installation.Id, x.Installation.AddonId, x.Package.Manifest.Name, x.Installation.Version, x.Installation.PackageSha256,
                x.Installation.TargetKind, x.Installation.TargetId, x.Installation.Capabilities, x.Installation.Network,
                x.Package.Repository, x.Installation.InstalledBy, x.Installation.InstalledAt,
                blocked(x.Installation.AddonId, x.Installation.PackageSha256)))
            .ToList();
    }
}

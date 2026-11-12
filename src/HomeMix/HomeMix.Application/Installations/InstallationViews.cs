using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations;

/// <summary>Views for a list of installations, with their packages read in one query.</summary>
internal static class InstallationViews
{
    public static async Task<IReadOnlyList<InstallationView>> ForAsync(
        IReadOnlyList<Installation> installations, IPackageRepository packages, CancellationToken cancellationToken)
    {
        var bySha = (await packages.FindManyAsync(installations.Select(i => i.PackageSha256).Distinct().ToList(), cancellationToken))
            .ToDictionary(p => p.Sha256, StringComparer.Ordinal);
        return installations
            .Where(i => bySha.ContainsKey(i.PackageSha256))
            .Select(i => InstallationView.From(i, bySha[i.PackageSha256]))
            .OrderBy(v => v.Name.Pl, StringComparer.CurrentCulture)
            .ToList();
    }
}

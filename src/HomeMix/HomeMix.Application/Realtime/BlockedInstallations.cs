using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Realtime;

/// <summary>Tells the users of every installation a block (an add-on id or a package's SHA-256) touches.</summary>
internal static class BlockedInstallations
{
    public static async Task AnnounceAsync(
        string value, string change, IInstallationRepository installations, RealtimeOutbox realtime, CancellationToken cancellationToken)
    {
        foreach (var installation in await installations.ListAllActiveAsync(cancellationToken))
        {
            if (string.Equals(installation.AddonId, value, StringComparison.Ordinal)
                || string.Equals(installation.PackageSha256, value, StringComparison.Ordinal))
            {
                realtime.Add(RealtimeEvents.InstallationChanged(installation, change));
            }
        }
    }
}

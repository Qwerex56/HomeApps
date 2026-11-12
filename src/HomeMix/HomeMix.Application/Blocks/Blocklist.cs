using HomeMix.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Blocks;

/// <summary>Blocks from both places: the configuration (<c>HomeMix:Blocked</c>) and the Owner's list in the web app.</summary>
public sealed class Blocklist(IBlockRepository blocks, IOptions<HomeMixOptions> options)
{
    public async Task<bool> IsBlockedAsync(string addonId, string sha256, CancellationToken cancellationToken) =>
        options.Value.IsBlocked(addonId, sha256) || await blocks.AnyAsync(addonId, sha256.ToLowerInvariant(), cancellationToken);

    /// <summary>Every block read once, for checking a whole list.</summary>
    public async Task<Func<string, string, bool>> SnapshotAsync(CancellationToken cancellationToken)
    {
        var owners = (await blocks.ListAsync(cancellationToken)).Select(b => b.Value).ToHashSet(StringComparer.Ordinal);
        var settings = options.Value;
        return (addonId, sha256) => settings.IsBlocked(addonId, sha256) || owners.Contains(addonId) || owners.Contains(sha256.ToLowerInvariant());
    }
}

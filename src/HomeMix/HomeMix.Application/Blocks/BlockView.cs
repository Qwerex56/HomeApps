using HomeMix.Domain.Blocks;

namespace HomeMix.Application.Blocks;

/// <param name="Value">An add-on id or a package SHA-256.</param>
/// <param name="FromConfiguration">From <c>HomeMix:Blocked</c>: shown, but lifted only in the configuration.</param>
public sealed record BlockView(string Value, bool IsSha256, string? Reason, Guid? BlockedBy, DateTimeOffset? BlockedAt, bool FromConfiguration)
{
    public static BlockView From(AddonBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return new BlockView(block.Value, AddonBlock.IsSha256(block.Value), block.Reason, block.BlockedBy, block.BlockedAt, false);
    }

    public static BlockView FromConfig(string value) => new(value, AddonBlock.IsSha256(value), null, null, null, true);
}

namespace HomeMix.Domain.Blocks;

/// <summary>
/// An add-on the Owner blocked from the web app, by its id (every version) or by one package's SHA-256. A blocked
/// add-on cannot be added or installed, and its installations stop being served and answer 404 until it is unblocked.
/// </summary>
public sealed class AddonBlock
{
    public const int MaxReasonLength = 300;

    private AddonBlock()
    {
    }

    /// <summary>An add-on id, or a package SHA-256 in lowercase hex.</summary>
    public string Value { get; private set; } = string.Empty;

    public string? Reason { get; private set; }

    public Guid BlockedBy { get; private set; }

    public DateTimeOffset BlockedAt { get; private set; }

    public static AddonBlock Create(string value, string? reason, Guid by, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new AddonBlock
        {
            Value = IsSha256(value) ? value.ToLowerInvariant() : value,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            BlockedBy = by,
            BlockedAt = now,
        };
    }

    public void Explain(string? reason) => Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

    public static bool IsSha256(string value) =>
        value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
}

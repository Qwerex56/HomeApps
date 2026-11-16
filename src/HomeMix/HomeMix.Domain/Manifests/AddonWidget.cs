namespace HomeMix.Domain.Manifests;

/// <summary>
/// A tile, with where it may stand (<c>Household</c>, <c>Wall</c>, <c>Account</c>), an optional own entry file, its
/// default <paramref name="Size"/> and every size it can render (<paramref name="Sizes"/>, the default among them).
/// </summary>
public sealed record AddonWidget(
    string Id, LocalizedText Title, string Size, IReadOnlyList<string> Places, string? Entry, IReadOnlyList<string> Sizes);

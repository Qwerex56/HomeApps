namespace HomeMix.Domain.Manifests;

/// <summary>A tile, with where it may stand (<c>Household</c>, <c>Wall</c>, <c>Account</c>) and an optional own entry file.</summary>
public sealed record AddonWidget(string Id, LocalizedText Title, string Size, IReadOnlyList<string> Places, string? Entry);

namespace HomeMix.Domain.Manifests;

/// <summary>
/// A tile, with where it may stand (<c>Household</c>, <c>Wall</c>, <c>Account</c>), an optional own entry file, its
/// default <paramref name="Size"/> and every size it can render (<paramref name="Sizes"/>, the default among them).
/// <paramref name="Devices"/> says which household devices it is for (household-jarvis-devices.md 2.5); empty when it needs none.
/// </summary>
public sealed record AddonWidget(
    string Id, LocalizedText Title, string Size, IReadOnlyList<string> Places, string? Entry, IReadOnlyList<string> Sizes,
    IReadOnlyList<DeviceMatch> Devices);

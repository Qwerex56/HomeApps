namespace HomeMix.Domain.Manifests;

/// <summary>What the bridge can offer an add-on (docs/homemix.md, "The bridge"). A new one is a change to that page.</summary>
public static class AddonCapabilities
{
    public const string StorageHousehold = "storage.household";
    public const string StorageUser = "storage.user";
    public const string HouseholdMembers = "household.members";
    public const string Notify = "notify";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { StorageHousehold, StorageUser, HouseholdMembers, Notify };
}

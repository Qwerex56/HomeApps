namespace HomeMix.Domain.Manifests;

/// <summary>What the bridge can offer an add-on (docs/homemix.md, "The bridge"). A new one is a change to that page.</summary>
public static class AddonCapabilities
{
    public const string StorageHousehold = "storage.household";
    public const string StorageUser = "storage.user";
    public const string HouseholdMembers = "household.members";
    public const string Notify = "notify";

    /// <summary>List and match the installation's household devices, read their state, subscribe to changes (docs/homemix-authoring.md, "Devices").</summary>
    public const string Devices = "devices";

    /// <summary>Change a device's controls; needs <see cref="Devices"/> as well.</summary>
    public const string DevicesControl = "devices.control";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal) { StorageHousehold, StorageUser, HouseholdMembers, Notify, Devices, DevicesControl };
}

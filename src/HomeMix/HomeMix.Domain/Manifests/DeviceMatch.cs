namespace HomeMix.Domain.Manifests;

/// <summary>
/// One way a widget finds its devices (<c>homemix.json</c> <c>widgets[].devices</c>, household-jarvis-devices.md 2.5):
/// by <paramref name="Capability"/> (what it can do), <paramref name="Role"/> (what it is for) or <paramref name="Model"/>;
/// every field given must match. <paramref name="Pick"/> <c>manual</c> makes the entry a last resort: its matching
/// devices are only a list for the household's admin to choose from.
/// </summary>
public sealed record DeviceMatch(string? Capability, string? Role, string? Model, string? Pick)
{
    public const string Manual = "manual";
    public const int MaxPerWidget = 8;

    public bool IsManual => Pick == Manual;
}

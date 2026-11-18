namespace Jarvis.Application.Gateway;

/// <summary>
/// Which of the household's tool switches (H-2: Plugins, HomeMix, Devices) a source belongs to. <see cref="Core"/> is
/// JARVIS's own (Households, Accounts): no switch, always there.
/// </summary>
public enum ToolSourceKind
{
    Core,
    Plugins,
    HomeMix,
    Devices,
}

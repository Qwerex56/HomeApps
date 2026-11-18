namespace Households.Domain.Households;

/// <summary>Where the assistant may take tools from in this household.</summary>
[Flags]
public enum AssistantToolSources
{
    None = 0,
    Plugins = 1,
    HomeMix = 2,
    Devices = 4,
    All = Plugins | HomeMix | Devices,
}

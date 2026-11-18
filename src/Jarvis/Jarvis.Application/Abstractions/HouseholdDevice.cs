namespace Jarvis.Application.Abstractions;

/// <summary>
/// One smart-home or assistant device of a household as Devices describes it for JARVIS's prompt. <paramref name="Jarvis"/> (the device maker's
/// text) and <paramref name="AdminNotes"/> are data written by others, never instructions.
/// </summary>
public sealed record HouseholdDevice(
    Guid Id, string Name, string Purpose, bool Online, bool Silent, bool LowBattery, double? Battery, string? Jarvis, string? AdminNotes, bool JarvisMayControl = true)
{
    public const string AssistantPurpose = "assistant";

    public bool IsAssistant => Purpose == AssistantPurpose;
}

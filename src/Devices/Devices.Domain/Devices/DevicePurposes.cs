namespace Devices.Domain.Devices;

/// <summary>The purposes as the descriptor spells them: <c>assistant</c>, <c>smart_home</c>, <c>computer</c>.</summary>
public static class DevicePurposes
{
    public static string ToWire(this DevicePurpose purpose) => purpose switch
    {
        DevicePurpose.Assistant => "assistant",
        DevicePurpose.SmartHome => "smart_home",
        DevicePurpose.Computer => "computer",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose)),
    };

    public static bool TryParse(string? wire, out DevicePurpose purpose)
    {
        switch (wire)
        {
            case "assistant": purpose = DevicePurpose.Assistant; return true;
            case "smart_home": purpose = DevicePurpose.SmartHome; return true;
            case "computer": purpose = DevicePurpose.Computer; return true;
            default: purpose = default; return false;
        }
    }

    /// <summary>A device that sends no descriptor (an old bridge): the purpose follows its kind.</summary>
    public static DevicePurpose FromKind(DeviceKind kind) => kind == DeviceKind.Satellite ? DevicePurpose.Assistant : DevicePurpose.Computer;
}

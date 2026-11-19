namespace Devices.Application.Abstractions;

/// <summary>
/// Remembers for a few seconds who asked a device to change a control (an add-on, D8), so the state change the device
/// reports next carries that source (<c>DeviceStateChanged.ChangedBy</c>) and the add-on does not react to its own set.
/// </summary>
public interface IControlSources
{
    /// <summary>How long a set stays attributed to its source.</summary>
    static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

    void Remember(Guid deviceId, string control, string source);

    /// <summary>The source of a recent set of any of these controls, or <c>null</c>.</summary>
    string? Find(Guid deviceId, IEnumerable<string> controls);
}

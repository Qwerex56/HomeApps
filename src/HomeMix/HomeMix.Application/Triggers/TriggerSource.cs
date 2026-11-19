namespace HomeMix.Application.Triggers;

/// <summary>
/// How Devices names an installation when it sets a control for it (<c>DeviceStateChanged.ChangedBy</c>). A change with
/// this source does not fire the same installation's triggers again: the loop guard (household-jarvis-devices.md 2.6).
/// </summary>
public static class TriggerSource
{
    public const string Prefix = "homemix:";

    public static string For(Guid installationId) => Prefix + installationId;
}

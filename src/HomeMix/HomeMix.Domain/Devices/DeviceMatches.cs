namespace HomeMix.Domain.Devices;

/// <summary>
/// The outcome of <see cref="DeviceMatcher"/>: every device that fits, best first, the one the widget shows
/// (<see cref="Selected"/>), and whether an admin still has to choose one (<see cref="NeedsPick"/>).
/// </summary>
public sealed record DeviceMatches(IReadOnlyList<DeviceCandidate> Candidates, Guid? Selected, bool NeedsPick);

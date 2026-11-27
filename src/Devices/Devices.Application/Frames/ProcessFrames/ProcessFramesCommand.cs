using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Frames.ProcessFrames;

/// <summary>Frames from the device <paramref name="DeviceId"/> (its own token's subject), from the link or from a report.</summary>
public sealed record ProcessFramesCommand(Guid DeviceId, IReadOnlyList<DeviceFrame> Frames) : ICommand<ReportResponseView>;

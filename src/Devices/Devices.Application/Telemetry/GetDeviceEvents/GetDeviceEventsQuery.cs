using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Telemetry.GetDeviceEvents;

/// <summary>A page of a device's events, newest first; household members only, not guests (D9).</summary>
public sealed record GetDeviceEventsQuery(Guid DeviceId, int Skip = 0, int Limit = GetDeviceEventsQuery.DefaultLimit) : IQuery<IReadOnlyList<DeviceEventView>>
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;
}

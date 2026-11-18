using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Telemetry.GetDeviceState;

/// <summary>The last state and health of a household device; any member of the household, a guest too (D6).</summary>
public sealed record GetDeviceStateQuery(Guid DeviceId) : IQuery<DeviceStateView>;

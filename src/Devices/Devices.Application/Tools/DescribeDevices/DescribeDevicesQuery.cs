using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.DescribeDevices;

/// <summary>
/// <c>devices_describe</c>: the full description of the smart-home and assistant devices in the caller's households (or in one of them),
/// for when JARVIS's prompt section was cut short.
/// </summary>
public sealed record DescribeDevicesQuery(Guid? HouseholdId) : IQuery<IReadOnlyList<DeviceDescriptionView>>;

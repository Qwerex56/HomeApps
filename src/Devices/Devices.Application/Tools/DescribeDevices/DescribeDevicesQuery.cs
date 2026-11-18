using Devices.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.DescribeDevices;

/// <summary>
/// <c>devices_describe</c>: the full description of the smart-home and assistant devices in one of the caller's households (the household is required, so the household ceiling applies),
/// for when JARVIS's prompt section was cut short.
/// </summary>
public sealed record DescribeDevicesQuery(Guid HouseholdId) : IQuery<IReadOnlyList<DeviceDescriptionView>>;

using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.DescribeDevices;

internal sealed class DescribeDevicesHandler(IDeviceRepository devices, IDeviceTelemetry telemetry, IHouseholdRoles households, IDeviceLinks links, ICurrentUser currentUser)
    : IQueryHandler<DescribeDevicesQuery, IReadOnlyList<DeviceDescriptionView>>
{
    public async Task<Result<IReadOnlyList<DeviceDescriptionView>>> Handle(DescribeDevicesQuery query, CancellationToken cancellationToken)
    {
        var reachable = await ReachableDevices.OfCallerAsync(devices, households, currentUser.RequiredUserId, cancellationToken);
        var chosen = reachable
            .Where(r => GeneratedTools.Applies(r.Device) && (query.HouseholdId is null || r.Device.HouseholdId == query.HouseholdId))
            .OrderBy(r => r.Device.Name, StringComparer.Ordinal)
            .ToList();
        var states = await telemetry.StatesAsync([.. chosen.Select(r => r.Device.Id)], cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceDescriptionView>>(
            [.. chosen.Select(r => DeviceDescriptionView.From(r, states.GetValueOrDefault(r.Device.Id), links.IsOnline(r.Device.Id)))]);
    }
}

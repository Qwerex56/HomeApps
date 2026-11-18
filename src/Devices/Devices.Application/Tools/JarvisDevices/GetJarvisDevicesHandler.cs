using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.JarvisDevices;

internal sealed class GetJarvisDevicesHandler(IDeviceRepository devices, IDeviceTelemetry telemetry, IDeviceLinks links, IHouseholdDirectory directory)
    : IQueryHandler<GetJarvisDevicesQuery, IReadOnlyList<DeviceDescriptionView>>
{
    public async Task<Result<IReadOnlyList<DeviceDescriptionView>>> Handle(GetJarvisDevicesQuery query, CancellationToken cancellationToken)
    {
        var all = (await devices.OfHouseholdsAsync([query.HouseholdId], cancellationToken)).Where(d => !d.IsDisabled).ToList();
        if (!all.Any(d => d.Id == query.UserId) && !(await directory.GetMemberIdsAsync(query.HouseholdId, cancellationToken)).Contains(query.UserId))
        {
            return Result.Ok<IReadOnlyList<DeviceDescriptionView>>([]);
        }

        var found = ReachableDevices.Name(all.Where(GeneratedTools.Applies));
        var states = await telemetry.StatesAsync([.. found.Select(r => r.Device.Id)], cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceDescriptionView>>(
            [.. found.OrderBy(r => r.Device.Name, StringComparer.Ordinal)
                .Select(r => DeviceDescriptionView.From(r, states.GetValueOrDefault(r.Device.Id), links.IsOnline(r.Device.Id)))]);
    }
}

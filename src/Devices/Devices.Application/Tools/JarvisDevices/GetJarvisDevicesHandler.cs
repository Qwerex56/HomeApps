using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.JarvisDevices;

internal sealed class GetJarvisDevicesHandler(IDeviceRepository devices, IDeviceTelemetry telemetry, IDeviceLinks links)
    : IQueryHandler<GetJarvisDevicesQuery, IReadOnlyList<DeviceDescriptionView>>
{
    public async Task<Result<IReadOnlyList<DeviceDescriptionView>>> Handle(GetJarvisDevicesQuery query, CancellationToken cancellationToken)
    {
        var found = ReachableDevices.Name((await devices.OfHouseholdsAsync([query.HouseholdId], cancellationToken))
            .Where(d => !d.IsDisabled && d.PairedAt is not null && GeneratedTools.Applies(d)));
        var states = await telemetry.StatesAsync([.. found.Select(r => r.Device.Id)], cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceDescriptionView>>(
            [.. found.OrderBy(r => r.Device.Name, StringComparer.Ordinal)
                .Select(r => DeviceDescriptionView.From(r, states.GetValueOrDefault(r.Device.Id), links.IsOnline(r.Device.Id)))]);
    }
}

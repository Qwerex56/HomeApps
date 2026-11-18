using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.MyDevices.GetMyDevices;

internal sealed class GetMyDevicesHandler(IDeviceRepository devices, IDeviceLinks links, ICurrentUser currentUser)
    : IQueryHandler<GetMyDevicesQuery, IReadOnlyList<DeviceView>>
{
    public async Task<Result<IReadOnlyList<DeviceView>>> Handle(GetMyDevicesQuery query, CancellationToken cancellationToken)
    {
        var mine = await devices.OfOwnerAsync(currentUser.RequiredUserId, cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceView>>(
            [.. mine.OrderBy(d => d.IsDisabled).ThenBy(d => d.Name, StringComparer.Ordinal).Select(d => d.ToView(links))]);
    }
}

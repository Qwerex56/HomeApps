using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.HouseholdDevices.GetHouseholdDevices;

internal sealed class GetHouseholdDevicesHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links)
    : IQueryHandler<GetHouseholdDevicesQuery, IReadOnlyList<DeviceView>>
{
    public async Task<Result<IReadOnlyList<DeviceView>>> Handle(GetHouseholdDevicesQuery query, CancellationToken cancellationToken)
    {
        var role = await households.GetRoleAsync(query.HouseholdId, cancellationToken);
        if (role is null)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        var found = await devices.OfHouseholdsAsync([query.HouseholdId], cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceView>>(
            [.. found.OrderBy(d => d.IsDisabled).ThenBy(d => d.Name, StringComparer.Ordinal).Select(d => d.ToView(links, role >= HouseholdRole.Admin))]);
    }
}

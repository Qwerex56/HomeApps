using System.Text.Json;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.HouseholdDevices.GetHouseholdDevices;

internal sealed class GetHouseholdDevicesHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, IDeviceTelemetry telemetry)
    : IQueryHandler<GetHouseholdDevicesQuery, IReadOnlyList<DeviceView>>
{
    public async Task<Result<IReadOnlyList<DeviceView>>> Handle(GetHouseholdDevicesQuery query, CancellationToken cancellationToken)
    {
        var role = await households.GetRoleAsync(query.HouseholdId, cancellationToken);
        if (role is null)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        var filter = new DeviceFilter(Clean(query.Capability), Clean(query.Role), Clean(query.Model));
        // 2.10: a removed (disabled) device is gone from the home; its history stays until the normal retention ends.
        var found = (await devices.OfHouseholdsAsync([query.HouseholdId], cancellationToken)).Where(d => !d.IsDisabled);
        if (!filter.IsEmpty)
        {
            found = found.Where(filter.Matches);
        }

        var ordered = found.OrderBy(d => d.Name, StringComparer.Ordinal).ToList();
        var states = query.WithState
            ? await telemetry.StatesAsync([.. ordered.Select(d => d.Id)], cancellationToken)
            : new Dictionary<Guid, DeviceState>();
        return Result.Ok<IReadOnlyList<DeviceView>>(
            [.. ordered.Select(d => d.ToView(links, role >= HouseholdRole.Admin, query.WithState ? StateOf(d, states, links, role) : null))]);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DeviceStateView StateOf(Device device, IReadOnlyDictionary<Guid, DeviceState> states, IDeviceLinks links, HouseholdRole? role)
    {
        states.TryGetValue(device.Id, out var state);
        var (values, valuesAt) = StateValues.Of(device, state, role);
        return new DeviceStateView(
            device.Id, values, valuesAt, !device.IsDisabled && links.IsOnline(device.Id), state?.Silent ?? false, state?.LowBattery ?? false,
            state?.LastStatusAt, state?.Battery, state?.Voltage, state?.Rssi, state?.Uptime, state?.Firmware, state?.Error);
    }
}

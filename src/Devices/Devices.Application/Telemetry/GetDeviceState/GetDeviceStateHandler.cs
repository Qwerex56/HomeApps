using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;

namespace Devices.Application.Telemetry.GetDeviceState;

internal sealed class GetDeviceStateHandler(IDeviceRepository devices, IDeviceTelemetry telemetry, IHouseholdRoles households, IDeviceLinks links)
    : IQueryHandler<GetDeviceStateQuery, DeviceStateView>
{
    public async Task<Result<DeviceStateView>> Handle(GetDeviceStateQuery query, CancellationToken cancellationToken)
    {
        var device = await devices.FindUsableAsync(households, query.DeviceId, HouseholdRole.Guest, DeviceErrors.NotFound, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        var state = await telemetry.FindStateAsync(device.Value.Id, cancellationToken);
        using var values = JsonDocument.Parse(state?.Values ?? "{}");
        return new DeviceStateView(
            device.Value.Id, values.RootElement.Clone(), state?.ValuesAt, links.IsOnline(device.Value.Id), state?.Silent ?? false, state?.LowBattery ?? false,
            state?.LastStatusAt, state?.Battery, state?.Voltage, state?.Rssi, state?.Uptime, state?.Firmware, state?.Error);
    }
}

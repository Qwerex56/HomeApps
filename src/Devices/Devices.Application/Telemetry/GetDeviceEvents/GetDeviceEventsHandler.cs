using System.Text.Json;
using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;

namespace Devices.Application.Telemetry.GetDeviceEvents;

internal sealed class GetDeviceEventsHandler(IDeviceRepository devices, IDeviceTelemetry telemetry, IHouseholdRoles households)
    : IQueryHandler<GetDeviceEventsQuery, IReadOnlyList<DeviceEventView>>
{
    public async Task<Result<IReadOnlyList<DeviceEventView>>> Handle(GetDeviceEventsQuery query, CancellationToken cancellationToken)
    {
        var device = await devices.FindUsableAsync(households, query.DeviceId, HouseholdRole.Member, DeviceErrors.MembersOnly, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        var events = await telemetry.EventsAsync(device.Value.Id, query.Skip, query.Limit, cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceEventView>>(
            [.. events.Select(e => new DeviceEventView(e.Id, e.Name, e.Data is null ? null : JsonDocument.Parse(e.Data).RootElement.Clone(), e.OccurredAt, e.IsLocation))]);
    }
}

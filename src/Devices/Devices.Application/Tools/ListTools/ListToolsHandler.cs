using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.ListTools;

internal sealed class ListToolsHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, ICurrentUser currentUser)
    : IQueryHandler<ListToolsQuery, IReadOnlyList<DeviceTool>>
{
    public async Task<Result<IReadOnlyList<DeviceTool>>> Handle(ListToolsQuery query, CancellationToken cancellationToken)
    {
        var reachable = await ReachableDevices.OfCallerAsync(devices, households, currentUser.RequiredUserId, cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceTool>>([
            .. reachable.Where(d => links.IsOnline(d.Id))
                .SelectMany(d => d.Actions.Select(a => new DeviceTool(
                    d.ToolName(a),
                    $"On the device \"{d.Name}\"{(d.IsHouseholdDevice ? " (household)" : string.Empty)}: {a.Description}".TrimEnd(' ', ':'),
                    a.InputSchema,
                    a.ReadOnly,
                    a.NeedsConfirmation,
                    d.HouseholdId))),
        ]);
    }
}

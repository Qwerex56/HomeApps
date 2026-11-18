using Devices.Application.Abstractions;
using Devices.Application.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.ListTools;

internal sealed class ListToolsHandler(IDeviceRepository devices, IDeviceLinks links, ICurrentUser currentUser)
    : IQueryHandler<ListToolsQuery, IReadOnlyList<DeviceTool>>
{
    public async Task<Result<IReadOnlyList<DeviceTool>>> Handle(ListToolsQuery query, CancellationToken cancellationToken)
    {
        var mine = await devices.OfOwnerAsync(currentUser.RequiredUserId, cancellationToken);
        return Result.Ok<IReadOnlyList<DeviceTool>>([
            .. mine.Where(d => !d.IsDisabled && links.IsOnline(d.Id))
                .OrderBy(d => d.Name, StringComparer.Ordinal)
                .SelectMany(d => d.Actions.Select(a => new DeviceTool(
                    d.ToolName(a),
                    $"On the device \"{d.Name}\": {a.Description}".TrimEnd(' ', ':'),
                    a.InputSchema,
                    a.ReadOnly,
                    a.NeedsConfirmation))),
        ]);
    }
}

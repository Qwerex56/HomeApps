using Devices.Application.Common;
using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Tools.CallTool;

internal sealed class CallToolHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, ICurrentUser currentUser)
    : ICommandHandler<CallToolCommand, string>
{
    public async Task<Result<string>> Handle(CallToolCommand command, CancellationToken cancellationToken)
    {
        // Device names have no '_', so the first one splits "mac-biuro_say_hello" into the device and its action.
        var split = command.Tool.IndexOf('_', StringComparison.Ordinal);
        if (split <= 0)
        {
            return Result.Fail(DeviceErrors.ActionNotFound);
        }

        var name = command.Tool[..split];
        var reachable = await ReachableDevices.OfCallerAsync(devices, households, currentUser.RequiredUserId, cancellationToken);
        var device = reachable.FirstOrDefault(d => d.Name == name);
        if (device is null)
        {
            return Result.Fail(DeviceErrors.NotFound);
        }

        var action = device.Find(command.Tool[(split + 1)..]);
        if (action is null)
        {
            return Result.Fail(DeviceErrors.ActionNotFound);
        }

        return await links.RunAsync(device.Id, action.Name, command.Input, cancellationToken);
    }
}

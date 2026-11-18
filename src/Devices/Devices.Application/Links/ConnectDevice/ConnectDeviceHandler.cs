using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Devices.Application.Links.ConnectDevice;

internal sealed class ConnectDeviceHandler(IDeviceRepository devices, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<ConnectDeviceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ConnectDeviceCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var ownerId = currentUser.RequiredUserId;
        var device = await devices.FindByNameAsync(ownerId, command.Name.Trim(), cancellationToken);
        if (device is null)
        {
            var created = Device.Create(ownerId, command.Name, command.Kind, now);
            if (created.IsFailed)
            {
                return created.ToResult();
            }

            device = created.Value;
            devices.Add(device);
        }

        var actions = command.Actions.Select(a => DeviceAction.Create(a.Name, a.Description, a.InputSchema, a.ReadOnly, a.Confirm)).ToList();
        var failed = actions.Find(a => a.IsFailed);
        if (failed is not null)
        {
            return failed.ToResult();
        }

        var connected = device.Connect([.. actions.Select(a => a.Value)], now);
        return connected.IsFailed ? connected : Result.Ok(device.Id);
    }
}

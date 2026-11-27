using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;

namespace Devices.Application.Controls.SetControl;

internal sealed class SetControlHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links, IDeviceTelemetry telemetry, IDeviceStream stream)
    : ICommandHandler<SetControlCommand>
{
    public async Task<Result> Handle(SetControlCommand command, CancellationToken cancellationToken)
    {
        var device = await devices.FindUsableAsync(households, command.DeviceId, HouseholdRole.Member, DeviceErrors.ReadOnly, cancellationToken);
        if (device.IsFailed)
        {
            return device.ToResult();
        }

        if (device.Value.Descriptor is null || DescriptorParser.Parse(device.Value.Descriptor) is not { IsSuccess: true } descriptor)
        {
            return Result.Fail(DeviceErrors.FramesUnsupported);
        }

        var valid = FrameValidation.CheckSet(descriptor.Value, command.Control, command.Value);
        if (valid.IsFailed)
        {
            return valid;
        }

        if (descriptor.Value.Sleepy)
        {
            // A sleepy device keeps no link: the value waits (last one wins) and goes out with the answer to its next report.
            var state = await telemetry.FindStateAsync(device.Value.Id, cancellationToken);
            if (state is null)
            {
                state = DeviceState.For(device.Value.Id);
                telemetry.Add(state);
            }

            state.SetPendingSetting(command.Control!, command.Value!.Value);
            stream.Publish(new DeviceStreamMessage(
                device.Value.HouseholdId!.Value, "changed", new { deviceId = device.Value.Id, reason = "pending_settings" }));
            return Result.Ok();
        }

        return await links.SetAsync(device.Value.Id, command.Control!, command.Value!.Value, cancellationToken);
    }
}

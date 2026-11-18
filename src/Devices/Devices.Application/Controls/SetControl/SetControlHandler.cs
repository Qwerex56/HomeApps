using Devices.Application.Abstractions;
using Devices.Application.Common;
using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;

namespace Devices.Application.Controls.SetControl;

internal sealed class SetControlHandler(IDeviceRepository devices, IHouseholdRoles households, IDeviceLinks links)
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
        return valid.IsFailed ? valid : await links.SetAsync(device.Value.Id, command.Control!, command.Value!.Value, cancellationToken);
    }
}

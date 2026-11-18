using Devices.Domain.Devices;
using FluentValidation;

namespace Devices.Application.HouseholdDevices.RenameDevice;

internal sealed class RenameDeviceValidator : AbstractValidator<RenameDeviceCommand>
{
    public RenameDeviceValidator()
    {
        RuleFor(c => c.DeviceId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(DeviceName.MaxLength * 4);
    }
}

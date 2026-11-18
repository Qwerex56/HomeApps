using Devices.Domain.Descriptors;
using FluentValidation;

namespace Devices.Application.HouseholdDevices.PairDevice;

internal sealed class PairDeviceValidator : AbstractValidator<PairDeviceCommand>
{
    public PairDeviceValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Descriptor).MaximumLength(DeviceDescriptor.MaxLength);
    }
}

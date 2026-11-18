using FluentValidation;

namespace Devices.Application.HouseholdDevices.PairDevice;

internal sealed class PairDeviceValidator : AbstractValidator<PairDeviceCommand>
{
    public PairDeviceValidator() => RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
}

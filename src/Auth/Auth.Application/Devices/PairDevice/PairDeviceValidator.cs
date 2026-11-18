using Auth.Domain.Devices;
using FluentValidation;

namespace Auth.Application.Devices.PairDevice;

internal sealed class PairDeviceValidator : AbstractValidator<PairDeviceCommand>
{
    public PairDeviceValidator()
    {
        RuleFor(c => c.DeviceId).NotEmpty();
        RuleFor(c => c.HouseholdId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(DeviceIdentity.NameMaxLength);
    }
}

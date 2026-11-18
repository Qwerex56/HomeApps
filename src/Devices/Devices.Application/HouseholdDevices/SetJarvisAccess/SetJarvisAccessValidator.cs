using FluentValidation;

namespace Devices.Application.HouseholdDevices.SetJarvisAccess;

internal sealed class SetJarvisAccessValidator : AbstractValidator<SetJarvisAccessCommand>
{
    public SetJarvisAccessValidator() => RuleFor(c => c.DeviceId).NotEmpty();
}

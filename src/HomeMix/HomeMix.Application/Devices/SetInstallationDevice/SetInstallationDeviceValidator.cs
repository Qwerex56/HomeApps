using FluentValidation;

namespace HomeMix.Application.Devices.SetInstallationDevice;

internal sealed class SetInstallationDeviceValidator : AbstractValidator<SetInstallationDeviceCommand>
{
    public SetInstallationDeviceValidator()
    {
        RuleFor(c => c.InstallationId).NotEmpty();
        RuleFor(c => c.DeviceId).NotEmpty();
        RuleFor(c => c.Control).NotEmpty().MaximumLength(100);
    }
}

using FluentValidation;

namespace HomeMix.Application.Devices.ChooseWidgetDevice;

internal sealed class ChooseWidgetDeviceValidator : AbstractValidator<ChooseWidgetDeviceCommand>
{
    public ChooseWidgetDeviceValidator()
    {
        RuleFor(c => c.InstallationId).NotEmpty();
        RuleFor(c => c.WidgetId).NotEmpty().MaximumLength(64);
    }
}

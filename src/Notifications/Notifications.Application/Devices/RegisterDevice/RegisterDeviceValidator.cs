using FluentValidation;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Devices.RegisterDevice;

internal sealed class RegisterDeviceValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceValidator()
    {
        RuleFor(c => c.Endpoint).NotEmpty().MaximumLength(PushTarget.MaxEndpointLength);
        RuleFor(c => c.P256dh).NotEmpty();
        RuleFor(c => c.Auth).NotEmpty();
    }
}

using FluentValidation;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Devices.ForgetDevice;

internal sealed class ForgetDeviceValidator : AbstractValidator<ForgetDeviceCommand>
{
    public ForgetDeviceValidator() => RuleFor(c => c.Endpoint).NotEmpty().MaximumLength(PushTarget.MaxEndpointLength);
}

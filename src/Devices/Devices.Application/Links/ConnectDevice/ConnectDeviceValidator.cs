using Devices.Domain.Descriptors;
using Devices.Domain.Devices;
using FluentValidation;

namespace Devices.Application.Links.ConnectDevice;

internal sealed class ConnectDeviceValidator : AbstractValidator<ConnectDeviceCommand>
{
    public ConnectDeviceValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(DeviceName.MaxLength);
        RuleFor(c => c.Descriptor).MaximumLength(DeviceDescriptor.MaxLength);
        RuleFor(c => c.Kind).IsInEnum();
        RuleFor(c => c.Actions).NotNull();
        RuleFor(c => c.Actions.Count).LessThanOrEqualTo(Device.MaxActions).When(c => c.Actions is not null);
    }
}

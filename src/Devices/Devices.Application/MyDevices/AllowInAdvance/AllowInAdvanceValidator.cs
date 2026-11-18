using Devices.Domain.Devices;
using FluentValidation;

namespace Devices.Application.MyDevices.AllowInAdvance;

internal sealed class AllowInAdvanceValidator : AbstractValidator<AllowInAdvanceCommand>
{
    public AllowInAdvanceValidator() => RuleFor(c => c.Action).NotEmpty().MaximumLength(DeviceAction.MaxNameLength);
}

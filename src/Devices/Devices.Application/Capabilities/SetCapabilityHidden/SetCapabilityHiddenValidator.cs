using FluentValidation;

namespace Devices.Application.Capabilities.SetCapabilityHidden;

internal sealed class SetCapabilityHiddenValidator : AbstractValidator<SetCapabilityHiddenCommand>
{
    public SetCapabilityHiddenValidator() => RuleFor(c => c.Name).NotEmpty().MaximumLength(70);
}

using FluentValidation;

namespace Devices.Application.Controls.SetControl;

internal sealed class SetControlValidator : AbstractValidator<SetControlCommand>
{
    public SetControlValidator()
    {
        RuleFor(c => c.Control).NotEmpty().MaximumLength(61);
    }
}

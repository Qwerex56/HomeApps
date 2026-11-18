using Auth.Domain.Identities;
using FluentValidation;

namespace Auth.Application.MyAccount.SetPin;

internal sealed class SetPinValidator : AbstractValidator<SetPinCommand>
{
    public SetPinValidator() =>
        RuleFor(c => c.Pin).Must(Identity.IsValidPin)
            .WithMessage($"The PIN must have {Identity.PinMinLength}–{Identity.PinMaxLength} digits.");
}

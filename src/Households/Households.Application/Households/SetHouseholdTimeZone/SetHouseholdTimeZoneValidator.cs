using FluentValidation;

namespace Households.Application.Households;

internal sealed class SetHouseholdTimeZoneValidator : AbstractValidator<SetHouseholdTimeZoneCommand>
{
    public SetHouseholdTimeZoneValidator() => RuleFor(c => c.TimeZone).NotNull();
}

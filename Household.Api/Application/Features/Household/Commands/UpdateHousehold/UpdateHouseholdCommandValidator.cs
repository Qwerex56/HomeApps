using FluentValidation;

namespace HouseholdService.Application.Features.Household.Commands.UpdateHousehold;

public class UpdateHouseholdCommandValidator : AbstractValidator<UpdateHouseholdCommand> {
    public UpdateHouseholdCommandValidator() {
        RuleFor(command => command.NewName)
            .NotEmpty()
            .NotNull()
            .MinimumLength(3)
            .MaximumLength(32);
        
        RuleFor(command => command.NewDescription)
            .NotEmpty()
            .NotNull()
            .MinimumLength(3)
            .MaximumLength(120);
    }
}
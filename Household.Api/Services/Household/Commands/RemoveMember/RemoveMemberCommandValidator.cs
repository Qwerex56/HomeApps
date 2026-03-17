using FluentValidation;
using FluentValidation.Results;

namespace Household.Api.Services.Household.Commands.RemoveMember;

internal class RemoveMemberCommandValidator : IValidator<RemoveMemberCommand> {
    public ValidationResult Validate(IValidationContext context) {
        throw new NotImplementedException();
    }

    public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = new CancellationToken()) {
        throw new NotImplementedException();
    }

    public IValidatorDescriptor CreateDescriptor() {
        throw new NotImplementedException();
    }

    public bool CanValidateInstancesOfType(Type type) {
        throw new NotImplementedException();
    }

    public ValidationResult Validate(RemoveMemberCommand instance) {
        throw new NotImplementedException();
    }

    public Task<ValidationResult> ValidateAsync(RemoveMemberCommand instance, CancellationToken cancellation = new CancellationToken()) {
        throw new NotImplementedException();
    }
}
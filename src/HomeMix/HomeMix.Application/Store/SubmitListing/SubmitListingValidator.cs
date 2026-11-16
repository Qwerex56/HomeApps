using FluentValidation;

namespace HomeMix.Application.Store.SubmitListing;

internal sealed class SubmitListingValidator : AbstractValidator<SubmitListingCommand>
{
    public SubmitListingValidator() => RuleFor(c => c.Repository).NotEmpty().MaximumLength(200);
}

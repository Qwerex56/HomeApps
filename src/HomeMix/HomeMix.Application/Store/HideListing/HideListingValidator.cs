using FluentValidation;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.HideListing;

internal sealed class HideListingValidator : AbstractValidator<HideListingCommand>
{
    public HideListingValidator() => RuleFor(c => c.Key).NotEmpty().MaximumLength(ListingKey.MaxLength);
}

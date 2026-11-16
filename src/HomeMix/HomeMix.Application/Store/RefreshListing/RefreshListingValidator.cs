using FluentValidation;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.RefreshListing;

internal sealed class RefreshListingValidator : AbstractValidator<RefreshListingCommand>
{
    public RefreshListingValidator() => RuleFor(c => c.Key).NotEmpty().MaximumLength(ListingKey.MaxLength);
}

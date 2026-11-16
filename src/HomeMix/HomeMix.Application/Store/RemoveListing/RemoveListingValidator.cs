using FluentValidation;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.RemoveListing;

internal sealed class RemoveListingValidator : AbstractValidator<RemoveListingCommand>
{
    public RemoveListingValidator() => RuleFor(c => c.Key).NotEmpty().MaximumLength(ListingKey.MaxLength);
}

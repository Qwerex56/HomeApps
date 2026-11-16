using FluentValidation;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.GiveHeart;

internal sealed class GiveHeartValidator : AbstractValidator<GiveHeartCommand>
{
    public GiveHeartValidator() => RuleFor(c => c.Key).NotEmpty().MaximumLength(ListingKey.MaxLength);
}

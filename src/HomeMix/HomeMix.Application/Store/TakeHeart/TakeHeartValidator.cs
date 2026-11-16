using FluentValidation;
using HomeMix.Domain.Store;

namespace HomeMix.Application.Store.TakeHeart;

internal sealed class TakeHeartValidator : AbstractValidator<TakeHeartCommand>
{
    public TakeHeartValidator() => RuleFor(c => c.Key).NotEmpty().MaximumLength(ListingKey.MaxLength);
}

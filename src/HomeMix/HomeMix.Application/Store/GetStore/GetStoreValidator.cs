using FluentValidation;

namespace HomeMix.Application.Store.GetStore;

internal sealed class GetStoreValidator : AbstractValidator<GetStoreQuery>
{
    public GetStoreValidator()
    {
        RuleFor(q => q.Tab).Must(t => t is null || StorePage.Tabs.Contains(t))
            .WithMessage($"Tab is one of: {string.Join(", ", StorePage.Tabs)}.");
        RuleFor(q => q.Target).Must(t => t is null || t is "household" or "user")
            .WithMessage("Target is household or user.");
        RuleFor(q => q.Q).MaximumLength(100);
        RuleFor(q => q.Page).InclusiveBetween(1, 1000);
    }
}

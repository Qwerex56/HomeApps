using FluentValidation;

namespace HomeMix.Application.Installations.ChooseWidget;

internal sealed class ChooseWidgetValidator : AbstractValidator<ChooseWidgetCommand>
{
    public ChooseWidgetValidator()
    {
        RuleFor(c => c.InstallationId).NotEmpty();
        RuleFor(c => c.Place).NotEmpty().MaximumLength(16);
        RuleFor(c => c.WidgetId).MaximumLength(64);
        RuleFor(c => c.Size).Must(s => s is null or "Small" or "Medium" or "Large").WithMessage("size is Small, Medium or Large.");
        RuleFor(c => c.Order).InclusiveBetween(0, 1000);
    }
}

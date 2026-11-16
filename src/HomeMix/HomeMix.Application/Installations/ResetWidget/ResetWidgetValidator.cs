using FluentValidation;

namespace HomeMix.Application.Installations.ResetWidget;

internal sealed class ResetWidgetValidator : AbstractValidator<ResetWidgetCommand>
{
    public ResetWidgetValidator()
    {
        RuleFor(c => c.InstallationId).NotEmpty();
        RuleFor(c => c.Place).NotEmpty().MaximumLength(16);
    }
}

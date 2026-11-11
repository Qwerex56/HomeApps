using FluentValidation;

namespace Auth.Application.ConnectedApps.DecideConsent;

internal sealed class DecideConsentValidator : AbstractValidator<DecideConsentCommand>
{
    public DecideConsentValidator()
    {
        RuleFor(c => c.Request).NotNull();
        RuleFor(c => c.Request.State).MaximumLength(1000);
    }
}

using FluentValidation;

namespace Auth.Application.ConnectedApps.RedeemCode;

internal sealed class RedeemCodeValidator : AbstractValidator<RedeemCodeCommand>
{
    public RedeemCodeValidator()
    {
        RuleFor(c => c.ClientId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.RedirectUri).NotEmpty();
        RuleFor(c => c.CodeVerifier).NotEmpty();
    }
}

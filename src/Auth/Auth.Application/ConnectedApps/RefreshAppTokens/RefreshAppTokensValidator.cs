using FluentValidation;

namespace Auth.Application.ConnectedApps.RefreshAppTokens;

internal sealed class RefreshAppTokensValidator : AbstractValidator<RefreshAppTokensCommand>
{
    public RefreshAppTokensValidator()
    {
        RuleFor(c => c.ClientId).NotEmpty();
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}

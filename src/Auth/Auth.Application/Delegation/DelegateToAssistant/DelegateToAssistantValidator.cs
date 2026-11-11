using FluentValidation;

namespace Auth.Application.Delegation.DelegateToAssistant;

internal sealed class DelegateToAssistantValidator : AbstractValidator<DelegateToAssistantCommand>
{
    public DelegateToAssistantValidator()
    {
        RuleFor(c => c.ClientId).NotEmpty().MaximumLength(100);
        RuleFor(c => c.ClientSecret).NotEmpty().MaximumLength(500);
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Channel).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Reference).MaximumLength(200);
    }
}

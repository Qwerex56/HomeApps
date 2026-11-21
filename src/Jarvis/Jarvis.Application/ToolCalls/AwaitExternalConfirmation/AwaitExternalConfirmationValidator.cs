using FluentValidation;

namespace Jarvis.Application.ToolCalls.AwaitExternalConfirmation;

internal sealed class AwaitExternalConfirmationValidator : AbstractValidator<AwaitExternalConfirmationCommand>
{
    public AwaitExternalConfirmationValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ClientId).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Tool).NotEmpty().MaximumLength(128);
    }
}

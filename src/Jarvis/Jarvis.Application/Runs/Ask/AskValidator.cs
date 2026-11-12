using FluentValidation;

namespace Jarvis.Application.Runs.Ask;

internal sealed class AskValidator : AbstractValidator<AskCommand>
{
    public AskValidator()
    {
        RuleFor(c => c.Text).NotEmpty().MaximumLength(AskCommand.MaxLength);
        RuleFor(c => c.Channel).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Reference).MaximumLength(200);
    }
}

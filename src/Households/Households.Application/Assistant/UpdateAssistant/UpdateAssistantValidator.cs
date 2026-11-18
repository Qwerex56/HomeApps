using FluentValidation;
using Households.Domain.Households;

namespace Households.Application.Assistant.UpdateAssistant;

internal sealed class UpdateAssistantValidator : AbstractValidator<UpdateAssistantCommand>
{
    public UpdateAssistantValidator()
    {
        RuleFor(c => c.AccessLevel).IsInEnum();
        RuleFor(c => c.ToolSources).Must(s => (s & ~AssistantToolSources.All) == 0);
    }
}

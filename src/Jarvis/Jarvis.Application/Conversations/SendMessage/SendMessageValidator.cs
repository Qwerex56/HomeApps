using FluentValidation;
using Jarvis.Application.Runs.Ask;

namespace Jarvis.Application.Conversations.SendMessage;

internal sealed class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(c => c.Text).NotEmpty().MaximumLength(AskCommand.MaxLength);
    }
}

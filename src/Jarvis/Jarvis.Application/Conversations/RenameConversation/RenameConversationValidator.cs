using FluentValidation;
using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Conversations.RenameConversation;

internal sealed class RenameConversationValidator : AbstractValidator<RenameConversationCommand>
{
    public RenameConversationValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(Conversation.TitleLength);
    }
}

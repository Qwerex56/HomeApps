using FluentValidation;
using Jarvis.Domain.Conversations;

namespace Jarvis.Application.Conversations.StartConversation;

internal sealed class StartConversationValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationValidator()
    {
        RuleFor(c => c.Title).MaximumLength(Conversation.TitleLength);
    }
}

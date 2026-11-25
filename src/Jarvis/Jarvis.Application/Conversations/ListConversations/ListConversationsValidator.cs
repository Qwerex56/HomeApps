using FluentValidation;

namespace Jarvis.Application.Conversations.ListConversations;

internal sealed class ListConversationsValidator : AbstractValidator<ListConversationsQuery>
{
    public const int SearchLength = 100;

    public ListConversationsValidator() => RuleFor(q => q.Search).MaximumLength(SearchLength);
}

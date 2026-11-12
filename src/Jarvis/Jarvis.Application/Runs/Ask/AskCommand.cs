using HomeApps.BuildingBlocks.Application.Messaging;
using Jarvis.Application.Common;

namespace Jarvis.Application.Runs.Ask;

/// <summary>
/// One request to JARVIS from any channel (jarvis-v1.md 2.1): <c>POST /jarvis/api/ask</c>, later the account's
/// conversation (J2c) and the chat (<c>AssistantInvoked</c>). The person is named, since a channel's message has no HTTP caller.
/// </summary>
public sealed record AskCommand(
    Guid UserId,
    string Text,
    Guid? HouseholdId,
    string Channel,
    string? Reference,
    IReadOnlyList<ContextLine> Context) : ICommand<AnswerView>
{
    public const int MaxLength = 4000;
}

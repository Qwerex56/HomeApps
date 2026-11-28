namespace Jarvis.Infrastructure.Brain;

/// <summary>The model connection in force right now: read on every turn, so a change needs no restart.</summary>
internal interface ILlmConnection
{
    Task<OpenAiChatOptions> CurrentAsync(CancellationToken cancellationToken);
}

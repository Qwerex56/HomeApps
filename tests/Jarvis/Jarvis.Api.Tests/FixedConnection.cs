using Jarvis.Infrastructure.Brain;

namespace Jarvis.Api.Tests;

/// <summary>A model connection that is always the same options (and can be changed between turns).</summary>
internal sealed class FixedConnection(OpenAiChatOptions options) : ILlmConnection
{
    public OpenAiChatOptions Options { get; set; } = options;

    public Task<OpenAiChatOptions> CurrentAsync(CancellationToken cancellationToken) => Task.FromResult(Options);
}

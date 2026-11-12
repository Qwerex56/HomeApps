namespace Jarvis.Application.Abstractions;

/// <param name="GatewayToken">The person's delegated token; with <paramref name="RunId"/> as <c>X-Jarvis-Run</c> the gateway logs every call to this run.</param>
public sealed record BrainRequest(Guid RunId, string SystemPrompt, string Prompt, string GatewayToken);

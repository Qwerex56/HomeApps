namespace Jarvis.Infrastructure.Brain;

/// <summary>The outcome of asking the model API for its models; <see cref="Error"/> is short and never carries the key.</summary>
public sealed record LlmCheckResult(bool Ok, string? Error);

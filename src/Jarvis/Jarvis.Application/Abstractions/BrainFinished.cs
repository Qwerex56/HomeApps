namespace Jarvis.Application.Abstractions;

/// <param name="Text">The whole answer.</param>
public sealed record BrainFinished(string Text, string? Model, decimal? CostUsd) : BrainEvent;

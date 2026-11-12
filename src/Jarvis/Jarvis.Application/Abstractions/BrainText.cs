namespace Jarvis.Application.Abstractions;

/// <summary>A piece of the answer, shown as it comes (SSE in the account's conversation, J2c).</summary>
public sealed record BrainText(string Text) : BrainEvent;

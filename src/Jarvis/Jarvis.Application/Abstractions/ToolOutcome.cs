namespace Jarvis.Application.Abstractions;

/// <summary>A tool's answer as text, and whether it is an error.</summary>
public sealed record ToolOutcome(string Text, bool Failed);

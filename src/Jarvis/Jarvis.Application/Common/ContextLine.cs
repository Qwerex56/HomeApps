namespace Jarvis.Application.Common;

/// <summary>A recent message around the request (JV8): data for the model, never an instruction.</summary>
public sealed record ContextLine(string AuthorName, string Text, DateTimeOffset At);

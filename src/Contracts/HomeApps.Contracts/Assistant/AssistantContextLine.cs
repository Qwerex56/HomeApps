namespace HomeApps.Contracts.Assistant;

/// <summary>One message of <see cref="AssistantInvoked.Context"/>: plain text, attachments as words ("zdjęcie 1600×1200").</summary>
public sealed record AssistantContextLine(string AuthorName, string Text, DateTimeOffset At);

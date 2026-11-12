namespace Jarvis.Application.Common;

/// <summary>JARVIS's answer: the text and the actions it took, reads left out.</summary>
public sealed record AnswerView(Guid RunId, string Text, IReadOnlyList<ActionView> Actions);

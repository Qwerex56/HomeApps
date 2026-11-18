namespace Jarvis.Application.Voice;

/// <param name="UserId">The person, when <see cref="Outcome"/> is <see cref="SpeakerOutcome.Known"/>.</param>
/// <param name="Name">How the household calls them, for "Słucham, Adrian."</param>
public sealed record SpeakerMatch(SpeakerOutcome Outcome, Guid UserId = default, string Name = "");

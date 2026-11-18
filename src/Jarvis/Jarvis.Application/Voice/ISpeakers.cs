namespace Jarvis.Application.Voice;

/// <summary>
/// Who of a household's people a satellite heard (jarvis-satellites.md S4): the household's members (Households, read
/// with the satellite's own token), narrowed by the spoken name, then Auth checks the PIN against them.
/// </summary>
public interface ISpeakers
{
    Task<SpeakerMatch> IdentifyAsync(Guid deviceId, Guid householdId, string? name, string pin, CancellationToken cancellationToken);
}

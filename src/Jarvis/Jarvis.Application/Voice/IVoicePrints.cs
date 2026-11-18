namespace Jarvis.Application.Voice;

/// <summary>
/// Recognising a household's person by their voice (jarvis-satellites.md S4). Only its place in the flow for now: the
/// server's implementation knows nobody, because a real one needs stronger hardware than the home server.
/// </summary>
public interface IVoicePrints
{
    /// <returns>The person, or <c>null</c> when the voice is nobody's the household taught it.</returns>
    Task<Guid?> RecognizeAsync(Guid householdId, ReadOnlyMemory<byte> audio, int sampleRate, CancellationToken cancellationToken);
}

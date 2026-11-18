using Jarvis.Application.Voice;

namespace Jarvis.Infrastructure.Voice;

/// <summary>No voice recognition on this server (S4): satellites learn who speaks only from a PIN.</summary>
internal sealed class NoVoicePrints : IVoicePrints
{
    public Task<Guid?> RecognizeAsync(Guid householdId, ReadOnlyMemory<byte> audio, int sampleRate, CancellationToken cancellationToken) =>
        Task.FromResult<Guid?>(null);
}

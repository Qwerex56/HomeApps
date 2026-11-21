namespace MacDeviceEmulator.Abstractions;

/// <summary>Local speech recognition source producing text transcripts without leaving the machine.</summary>
public interface ISpeechInput
{
    Task<RecognizedSpeech?> ReadNextAsync(CancellationToken cancellationToken = default);
}

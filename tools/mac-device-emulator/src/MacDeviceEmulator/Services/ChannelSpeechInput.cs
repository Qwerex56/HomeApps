using System.Threading.Channels;
using MacDeviceEmulator.Abstractions;

namespace MacDeviceEmulator.Services;

/// <summary>In-process channel speech input fed by interactive console text or external recognizers.</summary>
public sealed class ChannelSpeechInput(TimeProvider? timeProvider = null) : ISpeechInput
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Channel<RecognizedSpeech> _channel = Channel.CreateUnbounded<RecognizedSpeech>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public bool TryWrite(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return _channel.Writer.TryWrite(new RecognizedSpeech(text, _timeProvider.GetUtcNow()));
    }

    public void Complete() => _channel.Writer.TryComplete();

    public async Task<RecognizedSpeech?> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            if (_channel.Reader.TryRead(out var item))
            {
                return item;
            }
        }

        return null;
    }
}

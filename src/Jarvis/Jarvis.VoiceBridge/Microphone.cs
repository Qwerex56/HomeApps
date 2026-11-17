using System.Runtime.InteropServices;
using System.Threading.Channels;
using PortAudioSharp;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The default microphone through PortAudio, as 16 kHz mono frames of <see cref="WakeWord.FrameSamples"/> samples
/// (80 ms). The first run asks macOS for microphone access for the terminal.
/// </summary>
internal sealed class Microphone : IDisposable
{
    public const int SampleRate = 16_000;

    private readonly Channel<short[]> _frames = Channel.CreateBounded<short[]>(
        new BoundedChannelOptions(64) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    private readonly List<short> _pending = [];
    private readonly PortAudioSharp.Stream _stream;

    public Microphone()
    {
        PortAudio.Initialize();
        var device = PortAudio.DefaultInputDevice;
        if (device == PortAudio.NoDevice)
        {
            throw new InvalidOperationException("No microphone found.");
        }

        var info = PortAudio.GetDeviceInfo(device);
        Name = info.name;
        var input = new StreamParameters
        {
            device = device,
            channelCount = 1,
            sampleFormat = SampleFormat.Float32,
            suggestedLatency = info.defaultLowInputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero,
        };
        _stream = new PortAudioSharp.Stream(input, null, SampleRate, 0, StreamFlags.ClipOff, OnAudio, IntPtr.Zero);
        _stream.Start();
    }

    public string Name { get; }

    public ChannelReader<short[]> Frames => _frames.Reader;

    /// <summary>Drops what was captured meanwhile, e.g. the chime that just played.</summary>
    public void Drain()
    {
        while (_frames.Reader.TryRead(out _))
        {
        }
    }

    public void Dispose()
    {
        _stream.Stop();
        _stream.Dispose();
        PortAudio.Terminate();
    }

    private StreamCallbackResult OnAudio(
        IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags statusFlags, IntPtr userData)
    {
        var samples = new float[frameCount];
        Marshal.Copy(input, samples, 0, (int)frameCount);
        foreach (var sample in samples)
        {
            _pending.Add((short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue));
        }

        while (_pending.Count >= WakeWord.FrameSamples)
        {
            _frames.Writer.TryWrite(_pending.GetRange(0, WakeWord.FrameSamples).ToArray());
            _pending.RemoveRange(0, WakeWord.FrameSamples);
        }

        return StreamCallbackResult.Continue;
    }
}

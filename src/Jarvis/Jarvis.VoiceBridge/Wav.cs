using System.Buffers.Binary;

namespace Jarvis.VoiceBridge;

/// <summary>16-bit PCM in and out of WAV files: JARVIS's answers, the chimes, kept recordings and <c>--check-wake</c>.</summary>
internal static class Wav
{
    public static byte[] Write(ReadOnlySpan<short> samples, int sampleRate)
    {
        var data = samples.Length * 2;
        var wav = new byte[44 + data];
        var span = wav.AsSpan();
        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], 36 + data);
        "WAVEfmt "u8.CopyTo(span[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], 1);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], sampleRate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], 2);
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], 16);
        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], data);
        for (var i = 0; i < samples.Length; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(span[(44 + (2 * i))..], samples[i]);
        }

        return wav;
    }

    /// <summary>The samples of a 16-bit mono WAV (the first channel of a stereo one).</summary>
    public static (short[] Samples, int SampleRate) Read(byte[] wav)
    {
        var span = wav.AsSpan();
        if (span.Length < 12 || !span[..4].SequenceEqual("RIFF"u8) || !span[8..12].SequenceEqual("WAVE"u8))
        {
            throw new InvalidDataException("Not a WAV file.");
        }

        int rate = 16_000, channels = 1, bits = 16;
        for (var at = 12; at + 8 <= span.Length;)
        {
            var id = span.Slice(at, 4);
            var size = BinaryPrimitives.ReadInt32LittleEndian(span[(at + 4)..]);
            var body = span.Slice(at + 8, Math.Min(size, span.Length - at - 8));
            if (id.SequenceEqual("fmt "u8))
            {
                channels = BinaryPrimitives.ReadInt16LittleEndian(body[2..]);
                rate = BinaryPrimitives.ReadInt32LittleEndian(body[4..]);
                bits = BinaryPrimitives.ReadInt16LittleEndian(body[14..]);
            }
            else if (id.SequenceEqual("data"u8))
            {
                if (bits != 16)
                {
                    throw new InvalidDataException("Only 16-bit WAV files are supported.");
                }

                var samples = new short[body.Length / 2 / channels];
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] = BinaryPrimitives.ReadInt16LittleEndian(body[(i * 2 * channels)..]);
                }

                return (samples, rate);
            }

            at += 8 + size + (size % 2);
        }

        throw new InvalidDataException("The WAV file has no data.");
    }
}

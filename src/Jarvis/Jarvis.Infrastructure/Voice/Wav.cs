using System.Buffers.Binary;

namespace Jarvis.Infrastructure.Voice;

/// <summary>The 44-byte RIFF header in front of raw PCM, which speech engines expect as an uploaded file.</summary>
internal static class Wav
{
    public static byte[] FromPcm16(ReadOnlySpan<byte> pcm, int sampleRate)
    {
        const short channels = 1;
        const short bits = 16;
        var wav = new byte[44 + pcm.Length];
        var span = wav.AsSpan();
        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], 36 + pcm.Length);
        "WAVEfmt "u8.CopyTo(span[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], channels);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], sampleRate * channels * bits / 8);
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], channels * bits / 8);
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], bits);
        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], pcm.Length);
        pcm.CopyTo(span[44..]);
        return wav;
    }
}

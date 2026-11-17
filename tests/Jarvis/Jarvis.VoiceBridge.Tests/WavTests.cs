namespace Jarvis.VoiceBridge.Tests;

public sealed class WavTests
{
    [Fact]
    public void Samples_survive_a_round_trip()
    {
        short[] samples = [0, 1, -1, short.MaxValue, short.MinValue, 1234];

        var (read, rate) = Wav.Read(Wav.Write(samples, 22_050));

        Assert.Equal(samples, read);
        Assert.Equal(22_050, rate);
    }

    [Fact]
    public void The_chimes_are_short_16_khz_sounds()
    {
        var (samples, rate) = Wav.Read(Chime.Listening);

        Assert.Equal(16_000, rate);
        Assert.InRange(samples.Length / (double)rate, 0.1, 0.5);
    }

    [Fact]
    public void Something_else_than_a_wav_is_refused() =>
        Assert.Throws<InvalidDataException>(() => Wav.Read("not a wav file"u8.ToArray()));
}

namespace Jarvis.VoiceBridge.Tests;

/// <summary>80 ms frames of made-up sound: quiet room noise or a loud tone standing in for speech.</summary>
internal static class Frames
{
    public static short[] Quiet() => Fill(i => (short)((i % 7) - 3) * 30);

    public static short[] Speech() => Fill(i => (short)(4000 * Math.Sin(2 * Math.PI * 220 * i / 16_000.0)));

    public static IEnumerable<short[]> For(TimeSpan length, Func<short[]> frame) =>
        Enumerable.Range(0, (int)Math.Ceiling(length.TotalMilliseconds / 80)).Select(_ => frame());

    private static short[] Fill(Func<int, double> sample) =>
        Enumerable.Range(0, WakeWord.FrameSamples).Select(i => (short)sample(i)).ToArray();
}

namespace Jarvis.VoiceBridge;

/// <summary>Short tones instead of a light: rising = "słucham", falling = "mam, myślę".</summary>
internal static class Chime
{
    private const int Rate = 16_000;

    public static byte[] Listening { get; } = Tones(660, 880);

    public static byte[] Heard { get; } = Tones(880, 660);

    private static byte[] Tones(params double[] hertz)
    {
        const int each = Rate / 10;
        var samples = new short[each * hertz.Length];
        for (var tone = 0; tone < hertz.Length; tone++)
        {
            for (var i = 0; i < each; i++)
            {
                var fade = Math.Min(1.0, Math.Min(i, each - i) / 160.0);
                samples[(tone * each) + i] = (short)(6000 * fade * Math.Sin(2 * Math.PI * hertz[tone] * i / Rate));
            }
        }

        return Wav.Write(samples, Rate);
    }
}

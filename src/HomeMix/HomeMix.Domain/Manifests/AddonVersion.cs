using System.Text.RegularExpressions;

namespace HomeMix.Domain.Manifests;

/// <summary>Versions as <c>a.b.c</c> with an optional pre-release, which counts as its release (like the web app's plugin check).</summary>
public static partial class AddonVersion
{
    public static bool IsValid(string? version) => version is not null && Pattern().IsMatch(version);

    /// <summary>Whether <paramref name="version"/> is <paramref name="minimum"/> or newer.</summary>
    public static bool AtLeast(string version, string minimum)
    {
        var (a, b) = (Parse(version), Parse(minimum));
        for (var i = 0; i < 3; i++)
        {
            if (a[i] != b[i])
            {
                return a[i] > b[i];
            }
        }

        return true;
    }

    private static int[] Parse(string version) =>
        version.Split('-', '+')[0].Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).Concat([0, 0, 0]).Take(3).ToArray();

    [GeneratedRegex(@"^\d{1,5}\.\d{1,5}\.\d{1,5}(-[0-9A-Za-z.-]{1,32})?$")]
    private static partial Regex Pattern();
}

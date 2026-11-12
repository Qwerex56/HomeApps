using System.Text.RegularExpressions;

namespace HomeMix.Domain.Data;

/// <summary>Collection names and document keys: what fits a URL segment without escaping.</summary>
public static partial class DataNames
{
    public const int MaxLength = 64;

    public static bool IsValid(string? name) => name is not null && Pattern().IsMatch(name);

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")]
    private static partial Regex Pattern();
}

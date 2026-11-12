namespace HomeMix.Api.Mcp;

/// <summary>What one run of a tool script may use (docs/homemix.md, "JARVIS").</summary>
internal static class ScriptLimits
{
    /// <summary>The whole run, the script's own <c>ctx.fetch</c> calls included.</summary>
    public static readonly TimeSpan Time = TimeSpan.FromSeconds(5);

    public static readonly TimeSpan FetchTime = TimeSpan.FromSeconds(3);

    public const long Memory = 10 * 1024 * 1024;

    public const int Statements = 1_000_000;

    public const int Recursion = 64;

    public const int SourceBytes = 256 * 1024;

    public const int FetchBytes = 1024 * 1024;

    public const int Fetches = 5;

    /// <summary>Calls into <c>ctx</c> (data, members, fetch) in one run.</summary>
    public const int HostCalls = 100;
}

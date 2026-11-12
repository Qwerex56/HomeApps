using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Gateway;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// Runs <c>claude -p</c> per request with only the gateway as MCP server (the person's delegated token and the run's id
/// in its headers), no built-in tools, the JARVIS system prompt and a turn limit, and streams its answer. The MCP config
/// with the token is a temporary file readable only by this user, deleted afterwards.
/// </summary>
internal sealed partial class ClaudeCodeBrain(IOptions<ClaudeCodeOptions> options, ILogger<ClaudeCodeBrain> logger) : IAssistantBrain
{
    /// <summary>Everything Claude Code could do besides the gateway's tools.</summary>
    private static readonly string[] BuiltInTools =
        ["Bash", "Read", "Write", "Edit", "MultiEdit", "Glob", "Grep", "LS", "WebFetch", "WebSearch", "NotebookEdit", "Task", "TodoWrite"];

    public bool IsReady => !string.IsNullOrWhiteSpace(options.Value.OAuthToken);

    public async IAsyncEnumerable<BrainEvent> RunAsync(BrainRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var settings = options.Value;
        var config = Path.Combine(Path.GetTempPath(), $"jarvis-mcp-{request.RunId:N}.json");
        await File.WriteAllTextAsync(config, McpConfig(settings.GatewayUrl, request), cancellationToken);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(config, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Timeout);
        using var process = new Process { StartInfo = StartInfo(settings, request, config) };
        try
        {
            process.Start();
            var reader = new StreamJsonReader();
            while (await process.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
            {
                foreach (var said in reader.Read(line))
                {
                    yield return said;
                }
            }

            await process.WaitForExitAsync(timeout.Token);
            if (process.ExitCode != 0)
            {
                LogExited(process.ExitCode, await process.StandardError.ReadToEndAsync(CancellationToken.None));
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            File.Delete(config);
        }
    }

    internal static string McpConfig(string gatewayUrl, BrainRequest request) => JsonSerializer.Serialize(new
    {
        mcpServers = new Dictionary<string, object>
        {
            ["homeapps"] = new
            {
                type = "http",
                url = gatewayUrl,
                headers = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {request.GatewayToken}",
                    [RunHeader.Name] = request.RunId.ToString(),
                },
            },
        },
    });

    private static ProcessStartInfo StartInfo(ClaudeCodeOptions settings, BrainRequest request, string config)
    {
        var start = new ProcessStartInfo(settings.Command)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetTempPath(),
        };
        foreach (var argument in new[]
        {
            "-p", request.Prompt,
            "--output-format", "stream-json", "--verbose",
            "--mcp-config", config, "--strict-mcp-config",
            "--allowedTools", "mcp__homeapps",
            "--disallowedTools", string.Join(',', BuiltInTools),
            "--system-prompt", request.SystemPrompt,
            "--max-turns", settings.MaxTurns.ToString(System.Globalization.CultureInfo.InvariantCulture),
        })
        {
            start.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrWhiteSpace(settings.Model))
        {
            start.ArgumentList.Add("--model");
            start.ArgumentList.Add(settings.Model);
        }

        start.Environment["CLAUDE_CODE_OAUTH_TOKEN"] = settings.OAuthToken;
        start.Environment.Remove("ANTHROPIC_API_KEY");
        return start;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Claude Code exited with {ExitCode}: {Error}")]
    private partial void LogExited(int exitCode, string error);
}

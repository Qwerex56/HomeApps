using System.Diagnostics;
using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>Runs an action's command with <c>/bin/sh -c</c>; the input arrives as environment variables, never in the command text.</summary>
internal static class CommandRunner
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private const int MaxOutput = 2000;

    public static async Task<(bool Ok, string Text)> RunAsync(ComputerAction action, JsonElement? input, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("/bin/sh", ["-c", action.Run])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.Environment["JARVIS_INPUT"] = input?.GetRawText() ?? "{}";
        if (input is { ValueKind: JsonValueKind.Object } fields)
        {
            foreach (var field in fields.EnumerateObject())
            {
                start.Environment[$"JARVIS_{field.Name.ToUpperInvariant()}"] =
                    field.Value.ValueKind == JsonValueKind.String ? field.Value.GetString() : field.Value.GetRawText();
            }
        }

        using var process = Process.Start(start)!;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var errors = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            var text = (await output).Trim() is { Length: > 0 } said ? said : (await errors).Trim();
            text = text.Length > MaxOutput ? text[..MaxOutput] : text;
            return process.ExitCode == 0
                ? (true, text.Length > 0 ? text : "Zrobione.")
                : (false, $"Kod wyjścia {process.ExitCode}. {text}".Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            return (false, $"Polecenie nie skończyło się w {Timeout.TotalSeconds:0} s.");
        }
    }
}

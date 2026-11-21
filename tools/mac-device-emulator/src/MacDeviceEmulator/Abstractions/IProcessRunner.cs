namespace MacDeviceEmulator.Abstractions;

/// <summary>Abstraction for running system processes with arguments.</summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);
}

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

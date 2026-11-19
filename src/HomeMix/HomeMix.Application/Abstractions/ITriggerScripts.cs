using FluentResults;
using HomeMix.Application.Triggers;

namespace HomeMix.Application.Abstractions;

/// <summary>Runs a trigger's script (Jint, the Api) within the script limits; a failed result carries what went wrong.</summary>
public interface ITriggerScripts
{
    Task<Result> RunAsync(TriggerRun run, CancellationToken cancellationToken);
}

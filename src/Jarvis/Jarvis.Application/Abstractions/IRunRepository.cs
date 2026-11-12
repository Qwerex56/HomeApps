using Jarvis.Domain.Runs;

namespace Jarvis.Application.Abstractions;

public interface IRunRepository
{
    Task<AssistantRun?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>What all runs since <paramref name="since"/> cost, for the daily budget (JV11).</summary>
    Task<decimal> CostSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);

    void Add(AssistantRun run);

    /// <summary>JV12, in the database at once: texts of runs and their calls older than <paramref name="contentBefore"/> go, whole rows older than <paramref name="rowsBefore"/>.</summary>
    Task ApplyRetentionAsync(DateTimeOffset contentBefore, DateTimeOffset rowsBefore, CancellationToken cancellationToken);

    /// <summary>"Forget me": every run and call of the person.</summary>
    Task ForgetAsync(Guid userId, CancellationToken cancellationToken);
}

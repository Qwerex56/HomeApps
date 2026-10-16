namespace HomeApps.BuildingBlocks.Application.Abstractions;

/// <summary>The clock. Inject it instead of calling <see cref="DateTimeOffset.UtcNow"/> so tests can control time.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}

using HomeApps.BuildingBlocks.Application.Abstractions;

namespace HomeApps.BuildingBlocks.Infrastructure.Time;

internal sealed class SystemDateTimeProvider(TimeProvider timeProvider) : IDateTimeProvider
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}

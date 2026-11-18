using Devices.Domain.Devices;

namespace Devices.Application.Abstractions;

/// <summary>What household devices reported: their last state and status (one row each) and the history of their events.</summary>
public interface IDeviceTelemetry
{
    Task<DeviceState?> FindStateAsync(Guid deviceId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, DeviceState>> StatesAsync(IReadOnlyCollection<Guid> deviceIds, CancellationToken cancellationToken);

    void Add(DeviceState state);

    void Add(DeviceEvent deviceEvent);

    /// <summary>The device's events, newest first.</summary>
    Task<IReadOnlyList<DeviceEvent>> EventsAsync(Guid deviceId, int skip, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> HouseholdsWithEventsAsync(CancellationToken cancellationToken);

    /// <summary>Deletes the household's events older than <paramref name="cutoff"/>, and its location events older than <paramref name="locationCutoff"/>; returns how many went.</summary>
    Task<int> DeleteEventsAsync(Guid householdId, DateTimeOffset cutoff, DateTimeOffset locationCutoff, CancellationToken cancellationToken);
}

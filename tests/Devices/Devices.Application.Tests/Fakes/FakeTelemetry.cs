using Devices.Application.Abstractions;
using Devices.Domain.Devices;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeTelemetry : IDeviceTelemetry
{
    public Dictionary<Guid, DeviceState> States { get; } = [];

    public List<DeviceEvent> Events { get; } = [];

    public Task<DeviceState?> FindStateAsync(Guid deviceId, CancellationToken cancellationToken) =>
        Task.FromResult(States.GetValueOrDefault(deviceId));

    public Task<IReadOnlyDictionary<Guid, DeviceState>> StatesAsync(IReadOnlyCollection<Guid> deviceIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, DeviceState>>(States.Where(s => deviceIds.Contains(s.Key)).ToDictionary(s => s.Key, s => s.Value));

    public void Add(DeviceState state) => States[state.DeviceId] = state;

    public void Add(DeviceEvent deviceEvent) => Events.Add(deviceEvent);

    public Task<IReadOnlyList<DeviceEvent>> EventsAsync(Guid deviceId, int skip, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DeviceEvent>>([.. Events.Where(e => e.DeviceId == deviceId).OrderByDescending(e => e.OccurredAt).Skip(skip).Take(take)]);

    public Task<IReadOnlyList<Guid>> HouseholdsWithEventsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>([.. Events.Select(e => e.HouseholdId).Distinct()]);

    public Task<int> DeleteEventsAsync(Guid householdId, DateTimeOffset cutoff, DateTimeOffset locationCutoff, CancellationToken cancellationToken) =>
        Task.FromResult(Events.RemoveAll(e => e.HouseholdId == householdId && e.OccurredAt < (e.IsLocation ? locationCutoff : cutoff)));
}

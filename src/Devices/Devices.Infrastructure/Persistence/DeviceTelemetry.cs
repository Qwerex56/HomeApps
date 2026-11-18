using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence;

internal sealed class DeviceTelemetry(DevicesDbContext db) : IDeviceTelemetry
{
    public Task<DeviceState?> FindStateAsync(Guid deviceId, CancellationToken cancellationToken) =>
        db.DeviceStates.FirstOrDefaultAsync(s => s.DeviceId == deviceId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DeviceState>> StatesAsync(IReadOnlyCollection<Guid> deviceIds, CancellationToken cancellationToken) =>
        await db.DeviceStates.Where(s => deviceIds.Contains(s.DeviceId)).ToDictionaryAsync(s => s.DeviceId, cancellationToken);

    public void Add(DeviceState state) => db.DeviceStates.Add(state);

    public void Add(DeviceEvent deviceEvent) => db.DeviceEvents.Add(deviceEvent);

    public async Task<IReadOnlyList<DeviceEvent>> EventsAsync(Guid deviceId, int skip, int take, CancellationToken cancellationToken) =>
        await db.DeviceEvents.AsNoTracking().Where(e => e.DeviceId == deviceId)
            .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> HouseholdsWithEventsAsync(CancellationToken cancellationToken) =>
        await db.DeviceEvents.Select(e => e.HouseholdId).Distinct().ToListAsync(cancellationToken);

    public Task<int> DeleteEventsAsync(Guid householdId, DateTimeOffset cutoff, DateTimeOffset locationCutoff, CancellationToken cancellationToken) =>
        db.DeviceEvents
            .Where(e => e.HouseholdId == householdId && e.OccurredAt < (e.IsLocation ? locationCutoff : cutoff))
            .ExecuteDeleteAsync(cancellationToken);
}

using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence;

internal sealed class DeviceRepository(DevicesDbContext db) : IDeviceRepository
{
    public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Device?> FindByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken) =>
        db.Devices.FirstOrDefaultAsync(d => d.OwnerId == ownerId && d.HouseholdId == null && d.Name == name, cancellationToken);

    public Task<Device?> FindInHouseholdAsync(Guid householdId, string name, CancellationToken cancellationToken) =>
        db.Devices.FirstOrDefaultAsync(d => d.HouseholdId == householdId && d.Name == name, cancellationToken);

    public Task<Device?> FindByPairingCodeAsync(string pairingCodeHash, CancellationToken cancellationToken) =>
        db.Devices.FirstOrDefaultAsync(d => d.PairingCodeHash == pairingCodeHash, cancellationToken);

    public async Task<IReadOnlyList<Device>> OfOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.Devices.Where(d => d.OwnerId == ownerId && d.HouseholdId == null).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Device>> OfHouseholdsAsync(IReadOnlyCollection<Guid> householdIds, CancellationToken cancellationToken) =>
        await db.Devices.Where(d => d.HouseholdId != null && householdIds.Contains(d.HouseholdId.Value)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Device>> HeartbeatCandidatesAsync(CancellationToken cancellationToken) =>
        await db.Devices
            .Where(d => d.HouseholdId != null && d.PairedAt != null && d.DisabledAt == null && d.Descriptor != null && d.Purpose != DevicePurpose.Computer)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<string>> SlugsAsync(Guid ownerId, Guid? householdId, CancellationToken cancellationToken) =>
        await db.Devices
            .Where(d => householdId == null ? d.OwnerId == ownerId && d.HouseholdId == null : d.HouseholdId == householdId)
            .Select(d => d.Slug)
            .ToListAsync(cancellationToken);

    public void Add(Device device) => db.Devices.Add(device);

    public void Remove(Device device) => db.Devices.Remove(device);
}

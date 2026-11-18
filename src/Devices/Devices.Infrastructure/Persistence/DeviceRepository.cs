using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence;

internal sealed class DeviceRepository(DevicesDbContext db) : IDeviceRepository
{
    public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Device?> FindByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken) =>
        db.Devices.FirstOrDefaultAsync(d => d.OwnerId == ownerId && d.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Device>> OfOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await db.Devices.Where(d => d.OwnerId == ownerId).ToListAsync(cancellationToken);

    public void Add(Device device) => db.Devices.Add(device);

    public void Remove(Device device) => db.Devices.Remove(device);
}

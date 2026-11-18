using Auth.Application.Abstractions;
using Auth.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

internal sealed class DeviceIdentityRepository(AuthDbContext db) : IDeviceIdentityRepository
{
    public Task<DeviceIdentity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.DeviceIdentities.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Add(DeviceIdentity identity) => db.DeviceIdentities.Add(identity);
}

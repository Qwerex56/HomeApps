using Devices.Application.Abstractions;
using Devices.Domain.Capabilities;
using Microsoft.EntityFrameworkCore;

namespace Devices.Infrastructure.Persistence;

internal sealed class CapabilityRegistry(DevicesDbContext db) : ICapabilityRegistry
{
    public Task<RegisteredCapability?> FindAsync(string name, CancellationToken cancellationToken) =>
        db.RegisteredCapabilities.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

    public async Task<IReadOnlyList<RegisteredCapability>> ListAsync(CancellationToken cancellationToken) =>
        await db.RegisteredCapabilities.ToListAsync(cancellationToken);

    public void Add(RegisteredCapability capability) => db.RegisteredCapabilities.Add(capability);
}

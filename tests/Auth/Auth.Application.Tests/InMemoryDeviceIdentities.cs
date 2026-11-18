using Auth.Application.Abstractions;
using Auth.Domain.Devices;

namespace Auth.Application.Tests;

internal sealed class InMemoryDeviceIdentities : IDeviceIdentityRepository
{
    public List<DeviceIdentity> All { get; } = [];

    public Task<DeviceIdentity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(All.Find(d => d.Id == id));

    public void Add(DeviceIdentity identity) => All.Add(identity);
}

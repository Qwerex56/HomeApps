using Auth.Domain.Devices;

namespace Auth.Application.Abstractions;

/// <summary>Household satellites' identities (jarvis-satellites.md 3a, S13).</summary>
public interface IDeviceIdentityRepository
{
    Task<DeviceIdentity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(DeviceIdentity identity);
}

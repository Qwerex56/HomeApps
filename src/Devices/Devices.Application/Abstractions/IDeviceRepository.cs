using Devices.Domain.Devices;

namespace Devices.Application.Abstractions;

/// <summary>Devices with their actions; changes are saved by the unit of work after the command.</summary>
public interface IDeviceRepository
{
    Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<Device?> FindByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken);

    /// <summary>The owner's devices, removed ones too.</summary>
    Task<IReadOnlyList<Device>> OfOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    void Add(Device device);

    void Remove(Device device);
}

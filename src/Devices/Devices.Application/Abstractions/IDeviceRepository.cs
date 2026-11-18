using Devices.Domain.Devices;

namespace Devices.Application.Abstractions;

/// <summary>Devices with their actions; changes are saved by the unit of work after the command.</summary>
public interface IDeviceRepository
{
    Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The person's own (account) device with this name.</summary>
    Task<Device?> FindByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken);

    Task<Device?> FindInHouseholdAsync(Guid householdId, string name, CancellationToken cancellationToken);

    Task<Device?> FindByPairingCodeAsync(string pairingCodeHash, CancellationToken cancellationToken);

    /// <summary>The person's own (account) devices, removed ones too.</summary>
    Task<IReadOnlyList<Device>> OfOwnerAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>The households' devices, removed ones too.</summary>
    Task<IReadOnlyList<Device>> OfHouseholdsAsync(IReadOnlyCollection<Guid> householdIds, CancellationToken cancellationToken);

    void Add(Device device);

    void Remove(Device device);
}

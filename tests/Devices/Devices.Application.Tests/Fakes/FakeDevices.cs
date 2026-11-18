using Devices.Application.Abstractions;
using Devices.Domain.Devices;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeDevices : IDeviceRepository
{
    public List<Device> All { get; } = [];

    public Task<Device?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(All.Find(d => d.Id == id));

    public Task<Device?> FindByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Device?> FindInHouseholdAsync(Guid householdId, string name, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<Device?> FindByPairingCodeAsync(string pairingCodeHash, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Device>> OfOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Device>>([.. All.Where(d => !d.IsHouseholdDevice && d.OwnerId == ownerId)]);

    public Task<IReadOnlyList<Device>> OfHouseholdsAsync(IReadOnlyCollection<Guid> householdIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Device>>([.. All.Where(d => d.HouseholdId is { } h && householdIds.Contains(h))]);

    public Task<IReadOnlyList<Device>> HeartbeatCandidatesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Device>>([.. All.Where(d => d is { IsPairedSatellite: true, IsDisabled: false, Descriptor: not null } && d.Purpose != DevicePurpose.Computer)]);

    public Task<IReadOnlyCollection<string>> SlugsAsync(Guid ownerId, Guid? householdId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public void Add(Device device) => All.Add(device);

    public void Remove(Device device) => All.Remove(device);
}

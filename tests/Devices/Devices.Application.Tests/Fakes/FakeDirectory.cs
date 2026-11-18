using Devices.Application.Abstractions;
using Devices.Domain.Devices;

namespace Devices.Application.Tests.Fakes;

internal sealed class FakeDirectory : IHouseholdDirectory
{
    public List<Guid> Admins { get; } = [];

    public EventRetention Retention { get; set; } = EventRetention.Default;

    public bool Down { get; set; }

    public Task<IReadOnlyList<Guid>> GetAdminIdsAsync(Guid householdId, CancellationToken cancellationToken) =>
        Down ? throw new HttpRequestException("down") : Task.FromResult<IReadOnlyList<Guid>>([.. Admins]);

    public List<Guid> Members { get; } = [];

    public Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken) =>
        Down ? throw new HttpRequestException("down") : Task.FromResult<IReadOnlyList<Guid>>([.. Members]);

    public Task<EventRetention> GetEventRetentionAsync(Guid householdId, CancellationToken cancellationToken) =>
        Down ? throw new HttpRequestException("down") : Task.FromResult(Retention);
}

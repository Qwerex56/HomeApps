using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

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

    public List<Guid> Guests { get; } = [];

    public Task<HouseholdRole?> GetRoleAsync(Guid householdId, Guid userId, CancellationToken cancellationToken) =>
        Down ? throw new HttpRequestException("down")
            : Task.FromResult<HouseholdRole?>(Guests.Contains(userId) ? HouseholdRole.Guest : Members.Contains(userId) ? HouseholdRole.Member : null);

    public Task<EventRetention> GetEventRetentionAsync(Guid householdId, CancellationToken cancellationToken) =>
        Down ? throw new HttpRequestException("down") : Task.FromResult(Retention);
}

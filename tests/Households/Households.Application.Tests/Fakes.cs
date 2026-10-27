using Households.Application.Abstractions;
using Households.Domain.Common;
using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Domain.Users;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.BuildingBlocks.Domain;
using HomeApps.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Households.Application.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

/// <summary>Households and the read side over one list, like the single database behind them.</summary>
internal sealed class InMemoryHouseholds(InMemoryUsers users) : IHouseholdRepository, IHouseholdDirectory
{
    public List<Household> Items { get; } = [];

    public Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.Find(h => h.Id == id));

    public Task<Household?> GetByGuestCodeAsync(InviteCode code, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(h => h.GuestCode == code));

    public Task<IReadOnlyList<Household>> ListInvolvingAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Household>>(
            Items.Where(h => h.Members.Any(m => m.UserId == userId || m.InvitedBy == userId)).ToList());

    public Task<IReadOnlyList<Household>> ListWithExpiredGuestsAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Household>>(Items.Where(h => h.Members.Any(m => !m.HasAccess(now))).ToList());

    public void Add(Household household) => Items.Add(household);

    public Task<IReadOnlyList<MyHouseholdItem>> MyHouseholdsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MyHouseholdItem>>(Items
            .Where(h => h.Status == HouseholdStatus.Active)
            .Select(h => (h, m: h.MemberOf(userId)))
            .Where(x => x.m is not null && x.m.HasAccess(now))
            .OrderBy(x => x.h.Name.Value)
            .Select(x => new MyHouseholdItem(x.h.Id, x.h.Name.Value, x.h.Description?.Value, x.m!.Role, x.h.Members.Count, x.h.OwnershipReceivedAt))
            .ToList());

    public Task<IReadOnlyList<AdminHouseholdItem>> AllHouseholdsAsync(
        string? search, DateTimeOffset? before, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdminHouseholdItem>>(Items
            .Where(h => search is null || h.Name.Value.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Where(h => before is null || h.CreatedAt < before)
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .Select(h => new AdminHouseholdItem(
                h.Id, h.Name.Value, h.Status, h.CreatedAt, h.ArchivedAt, h.Members.Count, h.Owner?.UserId,
                h.Owner is { } o ? users.Items.Find(u => u.Id == o.UserId)?.DisplayName : null))
            .ToList());
}

internal sealed class InMemoryInvites : IInviteRepository
{
    public List<Invite> Items { get; } = [];

    public Task<Invite?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.Find(i => i.Id == id));

    public Task<Invite?> GetByCodeAsync(InviteCode code, CancellationToken cancellationToken) => Task.FromResult(Items.Find(i => i.Code == code));

    public Task<IReadOnlyList<Invite>> ListNotRevokedAsync(Guid householdId, Guid? createdBy, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Invite>>(Items
            .Where(i => i.HouseholdId == householdId && i.RevokedAt is null && (createdBy is null || i.CreatedBy == createdBy))
            .ToList());

    public Task<IReadOnlyList<Invite>> ListByCreatorAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Invite>>(Items.Where(i => i.CreatedBy == userId).ToList());

    public void Add(Invite invite) => Items.Add(invite);
}

internal sealed class InMemoryUsers : IUserCopies
{
    public List<UserCopy> Items { get; } = [];

    public Task<UserCopy?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Items.Find(u => u.Id == id));

    public Task<IReadOnlyDictionary<Guid, UserCopy>> GetManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, UserCopy>>(Items.Where(u => ids.Contains(u.Id)).ToDictionary(u => u.Id));

    public void Add(UserCopy user) => Items.Add(user);

    public void Remove(UserCopy user) => Items.Remove(user);
}

/// <summary>Like the kit's DbContext: dispatches the aggregates' domain events through MediatR before "saving".</summary>
internal sealed class FakeUnitOfWork(InMemoryHouseholds households, IServiceProvider services) : IUnitOfWork
{
    public int Saves { get; private set; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var publisher = services.GetRequiredService<IPublisher>();
        for (var round = 0; round < 10; round++)
        {
            var events = households.Items.SelectMany(h => h.DomainEvents).ToList();
            if (events.Count == 0)
            {
                break;
            }

            households.Items.ForEach(h => h.ClearDomainEvents());
            foreach (var domainEvent in events)
            {
                var notification = Activator.CreateInstance(typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)!;
                await publisher.Publish(notification, cancellationToken);
            }
        }

        Saves++;
        return 1;
    }
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public ServerRole Role { get; set; } = ServerRole.User;

    public DateTimeOffset? AuthenticatedAt { get; set; }

    public bool IsAuthenticated => UserId is not null;

    public string? Email => null;

    public IReadOnlyCollection<string> Roles => [Role.ToString()];

    public bool IsInRole(string role) => Roles.Contains(role);

    public Guid RequiredUserId => UserId ?? throw new InvalidOperationException("Not signed in.");
}

internal sealed class FakeEventBus : IEventBus
{
    public List<IIntegrationEvent> Published { get; } = [];

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IIntegrationEvent
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}

internal static class DomainEventsExtensions
{
    public static IEnumerable<IDomainEvent> Pending(this InMemoryHouseholds households) => households.Items.SelectMany(h => h.DomainEvents);
}

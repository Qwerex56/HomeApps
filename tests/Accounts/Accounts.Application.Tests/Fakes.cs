using Accounts.Application.Abstractions;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts;

namespace Accounts.Application.Tests;

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Both ports over one list, like the single database behind them.</summary>
internal sealed class InMemoryUsers : IUserRepository, IUserDirectory
{
    public List<User> Items { get; } = [];

    public List<ErasedUser> Erased { get; } = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Find(u => u.Id == id));

    public Task<bool> IsErasedAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Erased.Exists(e => e.Id == id));

    public void Add(User user) => Items.Add(user);

    public void Remove(User user) => Items.Remove(user);

    public void AddErased(ErasedUser erased) => Erased.Add(erased);

    public Task<UserPage> ListAsync(UserListFilter filter, CancellationToken cancellationToken)
    {
        var query = Items.AsEnumerable();
        if (filter.Status is { } status) query = query.Where(u => u.Status == status);
        if (filter.Role is { } role) query = query.Where(u => u.Role == role);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(u =>
                u.Profile.DisplayName.Value.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || u.Email.Value.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
        }

        Func<User, object?> key = filter.Sort switch
        {
            UserSort.Email => u => u.Email.Value,
            UserSort.Role => u => u.Role,
            UserSort.Status => u => u.Status,
            UserSort.CreatedAt => u => u.CreatedAt,
            UserSort.LastSeenAt => u => u.LastSeenAt,
            _ => u => u.Profile.DisplayName.Value,
        };
        var all = (filter.Descending ? query.OrderByDescending(key) : query.OrderBy(key)).ToList();
        var items = all
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserListItem(u.Id, u.Profile.DisplayName.Value, u.Email.Value, u.Role, u.Status, u.CreatedAt, u.LastSeenAt))
            .ToList();
        return Task.FromResult(new UserPage(items, all.Count, filter.Page, filter.PageSize));
    }

    public Task<IReadOnlyList<HistoryEntry>> HistoryAsync(Guid userId, DateTimeOffset? before, int limit, CancellationToken cancellationToken)
    {
        IReadOnlyList<HistoryEntry> list = Items.Where(u => u.Id == userId)
            .SelectMany(u => u.History)
            .Where(h => before is null || h.At < before)
            .OrderByDescending(h => h.At)
            .ThenByDescending(h => h.Id)
            .Take(limit)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyDictionary<Guid, string>> DisplayNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, string> names = Items.Where(u => ids.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Profile.DisplayName.Value);
        return Task.FromResult(names);
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.FromResult(1);
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

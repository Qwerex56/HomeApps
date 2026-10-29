using Accounts.Domain.Common;
using Accounts.Domain.Users;

namespace Accounts.Application.Abstractions;

/// <summary>
/// Read-only queries for the admin screens and the history, projected straight from the database without loading
/// aggregates. One port for these screens, not a generic repository.
/// </summary>
public interface IUserDirectory
{
    Task<UserPage> ListAsync(UserListFilter filter, CancellationToken cancellationToken);

    /// <summary>Newest first, at most <paramref name="limit"/> entries older than <paramref name="before"/> when given.</summary>
    Task<IReadOnlyList<HistoryEntry>> HistoryAsync(Guid userId, DateTimeOffset? before, int limit, CancellationToken cancellationToken);

    /// <summary>Display names of the given users; erased or unknown ids are missing from the result.</summary>
    Task<IReadOnlyDictionary<Guid, string>> DisplayNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public enum UserSort
{
    DisplayName = 0,
    Email = 1,
    Role = 2,
    Status = 3,
    CreatedAt = 4,
    LastSeenAt = 5,
}

/// <summary>UC-10 filter. <see cref="Search"/> matches the display name or email, case-insensitive (Q4). <see cref="Page"/> starts at 1.</summary>
public sealed record UserListFilter(
    AccountStatus? Status,
    ServerRole? Role,
    string? Search,
    int Page,
    int PageSize,
    UserSort Sort,
    bool Descending);

public sealed record UserListItem(
    Guid Id,
    string DisplayName,
    string Email,
    ServerRole Role,
    AccountStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt);

public sealed record UserPage(IReadOnlyList<UserListItem> Items, int Total, int Page, int PageSize);

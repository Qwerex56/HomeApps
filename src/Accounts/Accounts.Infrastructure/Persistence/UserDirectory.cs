using Accounts.Application.Abstractions;
using System.Linq.Expressions;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Persistence;

/// <summary>The admin list and the history, read without tracking and projected in the database (plan 3.2).</summary>
internal sealed class UserDirectory(AccountsDbContext db) : IUserDirectory
{
    public async Task<UserPage> ListAsync(UserListFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var users = db.Users.AsNoTracking();
        if (filter.Status is { } status)
        {
            users = users.Where(u => u.Status == status);
        }

        if (filter.Role is { } role)
        {
            users = users.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            // Q4: ILIKE '%q%' on the name and the email; % and _ typed by the admin match themselves.
            var pattern = $"%{EscapeLike(filter.Search.Trim())}%";
            users = users.Where(u =>
                EF.Functions.ILike((string)(object)u.Profile.DisplayName, pattern, @"\") ||
                EF.Functions.ILike((string)(object)u.Email, pattern, @"\"));
        }

        var total = await users.CountAsync(cancellationToken);
        var items = await Sort(users, filter.Sort, filter.Descending)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new UserListItem(u.Id, u.Profile.DisplayName.Value, u.Email.Value, u.Role, u.Status, u.CreatedAt, u.LastSeenAt))
            .ToListAsync(cancellationToken);

        return new UserPage(items, total, filter.Page, filter.PageSize);
    }

    public async Task<IReadOnlyList<HistoryEntry>> HistoryAsync(
        Guid userId, DateTimeOffset? before, int limit, CancellationToken cancellationToken)
    {
        var entries = db.History.AsNoTracking().Where(h => h.UserId == userId);
        if (before is { } cursor)
        {
            entries = entries.Where(h => h.At < cursor);
        }

        return await entries.OrderByDescending(h => h.At).ThenByDescending(h => h.Id).Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> DisplayNamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var names = await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Profile.DisplayName })
            .ToListAsync(cancellationToken);
        return names.ToDictionary(n => n.Id, n => n.DisplayName.Value);
    }

    private static IQueryable<User> Sort(IQueryable<User> users, UserSort sort, bool descending)
    {
        IOrderedQueryable<User> sorted = (sort, descending) switch
        {
            (UserSort.Email, false) => users.OrderBy(u => u.Email),
            (UserSort.Email, true) => users.OrderByDescending(u => u.Email),
            (UserSort.Role, false) => users.OrderBy(RoleRank),
            (UserSort.Role, true) => users.OrderByDescending(RoleRank),
            (UserSort.Status, false) => users.OrderBy(u => u.Status),
            (UserSort.Status, true) => users.OrderByDescending(u => u.Status),
            (UserSort.CreatedAt, false) => users.OrderBy(u => u.CreatedAt),
            (UserSort.CreatedAt, true) => users.OrderByDescending(u => u.CreatedAt),
            (UserSort.LastSeenAt, false) => users.OrderBy(u => u.LastSeenAt),
            (UserSort.LastSeenAt, true) => users.OrderByDescending(u => u.LastSeenAt),
            (_, false) => users.OrderBy(u => u.Profile.DisplayName),
            (_, true) => users.OrderByDescending(u => u.Profile.DisplayName),
        };

        // A stable order, so pages never repeat or skip a user.
        return sorted.ThenBy(u => u.Id);
    }

    /// <summary>Roles are stored as names, so sort by rank (User &lt; Admin &lt; Owner), not alphabetically.</summary>
    private static readonly Expression<Func<User, int>> RoleRank =
        u => u.Role == ServerRole.Owner ? 2 : u.Role == ServerRole.Admin ? 1 : 0;

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}

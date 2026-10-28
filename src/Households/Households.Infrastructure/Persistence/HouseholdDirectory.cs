using Households.Application.Abstractions;
using Households.Domain.Households;
using Microsoft.EntityFrameworkCore;

namespace Households.Infrastructure.Persistence;

/// <summary>"My households" and the admin list, read without tracking and projected in the database.</summary>
internal sealed class HouseholdDirectory(HouseholdsDbContext db) : IHouseholdDirectory
{
    public async Task<IReadOnlyList<MyHouseholdItem>> MyHouseholdsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = await (
                from m in db.Memberships.AsNoTracking()
                join h in db.Households.IgnoreAutoIncludes().AsNoTracking() on m.HouseholdId equals h.Id
                where m.UserId == userId
                    && h.Status == HouseholdStatus.Active
                    && (m.AccessUntil == null || m.AccessUntil > now)
                select new
                {
                    h.Id,
                    h.Name,
                    h.Description,
                    m.Role,
                    Count = db.Memberships.Count(x => x.HouseholdId == h.Id),
                    h.OwnershipReceivedAt,
                })
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(r => r.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new MyHouseholdItem(r.Id, r.Name.Value, r.Description?.Value, r.Role, r.Count, r.OwnershipReceivedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<AdminHouseholdItem>> AllHouseholdsAsync(
        string? search, DateTimeOffset? before, int limit, CancellationToken cancellationToken)
    {
        var households = db.Households.IgnoreAutoIncludes().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Q7: ILIKE '%q%' on the name; % and _ typed by the admin match themselves.
            var pattern = $"%{EscapeLike(search.Trim())}%";
            households = households.Where(h => EF.Functions.ILike((string)(object)h.Name, pattern, @"\"));
        }

        if (before is { } cursor)
        {
            households = households.Where(h => h.CreatedAt < cursor);
        }

        var rows = await households
            .OrderByDescending(h => h.CreatedAt)
            .ThenByDescending(h => h.Id)
            .Take(limit)
            .Select(h => new
            {
                h.Id,
                h.Name,
                h.Status,
                h.CreatedAt,
                h.ArchivedAt,
                Count = db.Memberships.Count(m => m.HouseholdId == h.Id),
                OwnerId = db.Memberships
                    .Where(m => m.HouseholdId == h.Id && m.Role == HouseholdRole.Owner)
                    .Select(m => (Guid?)m.UserId)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var ownerIds = rows.Where(r => r.OwnerId is not null).Select(r => r.OwnerId!.Value).Distinct().ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        return rows
            .Select(r => new AdminHouseholdItem(
                r.Id, r.Name.Value, r.Status, r.CreatedAt, r.ArchivedAt, r.Count, r.OwnerId,
                r.OwnerId is { } owner && names.TryGetValue(owner, out var name) ? name : null))
            .ToList();
    }

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
}

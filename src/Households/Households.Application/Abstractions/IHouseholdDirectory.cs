using Households.Domain.Households;

namespace Households.Application.Abstractions;

/// <summary>Read-only lists projected straight from the database, without loading aggregates.</summary>
public interface IHouseholdDirectory
{
    /// <summary>H2: active households where the user has access, with their role; no paging (Q7).</summary>
    Task<IReadOnlyList<MyHouseholdItem>> MyHouseholdsAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Q7: every household, newest first, name matching <paramref name="search"/> (case-insensitive), created before <paramref name="before"/>.</summary>
    Task<IReadOnlyList<AdminHouseholdItem>> AllHouseholdsAsync(
        string? search, DateTimeOffset? before, int limit, CancellationToken cancellationToken);
}

public sealed record MyHouseholdItem(
    Guid Id,
    string Name,
    string? Description,
    HouseholdRole MyRole,
    int MemberCount,
    DateTimeOffset? OwnershipReceivedAt);

public sealed record AdminHouseholdItem(
    Guid Id,
    string Name,
    HouseholdStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt,
    int MemberCount,
    Guid? OwnerId,
    string? OwnerName);

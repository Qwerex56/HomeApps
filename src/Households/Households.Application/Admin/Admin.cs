using Households.Application.Abstractions;
using Households.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Households.Application.Admin;

/// <summary>Q7, D8: every household for server admins, newest first, 50 at a time; the next page passes the last <c>CreatedAt</c> as <see cref="Before"/>.</summary>
public sealed record ListAllHouseholdsQuery(string? Search, DateTimeOffset? Before) : IQuery<IReadOnlyList<AdminHouseholdView>>
{
    public const int Limit = 50;
}

internal sealed class ListAllHouseholdsValidator : AbstractValidator<ListAllHouseholdsQuery>
{
    public ListAllHouseholdsValidator() => RuleFor(q => q.Search).MaximumLength(64);
}

internal sealed class ListAllHouseholdsHandler(IHouseholdDirectory directory, ICurrentUser currentUser)
    : IQueryHandler<ListAllHouseholdsQuery, IReadOnlyList<AdminHouseholdView>>
{
    private static Error Forbidden => new AppError("forbidden.role", "Only server admins see every household.", ErrorKind.Forbidden);

    public async Task<Result<IReadOnlyList<AdminHouseholdView>>> Handle(ListAllHouseholdsQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.Actor().IsServerAdmin)
        {
            return Result.Fail<IReadOnlyList<AdminHouseholdView>>(Forbidden);
        }

        var items = await directory.AllHouseholdsAsync(query.Search, query.Before, ListAllHouseholdsQuery.Limit, cancellationToken);
        return items
            .Select(i => new AdminHouseholdView(i.Id, i.Name, i.Status, i.CreatedAt, i.ArchivedAt, i.MemberCount, i.OwnerId, i.OwnerName))
            .ToList();
    }
}

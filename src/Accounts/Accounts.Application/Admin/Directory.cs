using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Common;
using Accounts.Domain.Users;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.Admin;

/// <summary>UC-10: the account list for Admin and Owner, filtered, searched (Q4) and paged.</summary>
public sealed record ListUsersQuery(
    AccountStatus? Status = null,
    ServerRole? Role = null,
    string? Search = null,
    int Page = 1,
    int PageSize = ListUsersQuery.DefaultPageSize,
    UserSort Sort = UserSort.DisplayName,
    bool Descending = false) : IQuery<UserPage>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int SearchMaxLength = 100;
}

internal sealed class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersValidator()
    {
        RuleFor(q => q.Status).IsInEnum();
        RuleFor(q => q.Role).IsInEnum();
        RuleFor(q => q.Sort).IsInEnum();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, ListUsersQuery.MaxPageSize);
        RuleFor(q => q.Search).MaximumLength(ListUsersQuery.SearchMaxLength);
    }
}

internal sealed class ListUsersHandler(IUserDirectory directory, ICurrentUser currentUser) : IQueryHandler<ListUsersQuery, UserPage>
{
    public async Task<Result<UserPage>> Handle(ListUsersQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin())
        {
            return Result.Fail<UserPage>(UserErrors.ForbiddenRole);
        }

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        return await directory.ListAsync(
            new UserListFilter(query.Status, query.Role, search, query.Page, query.PageSize, query.Sort, query.Descending),
            cancellationToken);
    }
}

/// <summary>UC-11: everything about one account, including who disabled it and when it was last seen.</summary>
public sealed record GetUserQuery(Guid UserId) : IQuery<UserView>;

internal sealed class GetUserHandler(IUserRepository users, ICurrentUser currentUser) : IQueryHandler<GetUserQuery, UserView>
{
    public async Task<Result<UserView>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin())
        {
            return Result.Fail<UserView>(UserErrors.ForbiddenRole);
        }

        var user = await users.GetByIdAsync(query.UserId, cancellationToken);
        return user is null ? Result.Fail<UserView>(ApplicationErrors.UserNotFound) : user.ToAdminView();
    }
}

/// <summary>
/// UC-16, Q6: the full audit history, newest first, <see cref="Limit"/> at a time; older entries with
/// <see cref="Before"/> set to the oldest <c>At</c> already shown.
/// </summary>
public sealed record GetUserHistoryQuery(Guid UserId, DateTimeOffset? Before = null) : IQuery<IReadOnlyList<HistoryEntryView>>
{
    public const int Limit = 100;
}

internal sealed class GetUserHistoryHandler(IUserRepository users, IUserDirectory directory, ICurrentUser currentUser)
    : IQueryHandler<GetUserHistoryQuery, IReadOnlyList<HistoryEntryView>>
{
    public async Task<Result<IReadOnlyList<HistoryEntryView>>> Handle(GetUserHistoryQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAdmin())
        {
            return Result.Fail<IReadOnlyList<HistoryEntryView>>(UserErrors.ForbiddenRole);
        }

        if (await users.GetByIdAsync(query.UserId, cancellationToken) is null)
        {
            return Result.Fail<IReadOnlyList<HistoryEntryView>>(ApplicationErrors.UserNotFound);
        }

        var entries = await directory.HistoryAsync(query.UserId, query.Before, GetUserHistoryQuery.Limit, cancellationToken);
        var actorIds = entries.Where(e => e.ActorId is not null).Select(e => e.ActorId!.Value).Distinct().ToList();
        var names = await directory.DisplayNamesAsync(actorIds, cancellationToken);

        return entries
            .Select(e => new HistoryEntryView(
                e.At,
                e.Action,
                e.ActorId,
                e.ActorId is { } id && names.TryGetValue(id, out var name) ? name : null,
                e.Reason,
                e.Details))
            .ToList();
    }
}

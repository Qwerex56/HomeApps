using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

internal sealed class GetMyHistoryHandler(IUserDirectory directory, ICurrentUser currentUser)
    : IQueryHandler<GetMyHistoryQuery, IReadOnlyList<MyHistoryEntryView>>
{
    public async Task<Result<IReadOnlyList<MyHistoryEntryView>>> Handle(GetMyHistoryQuery query, CancellationToken cancellationToken)
    {
        var entries = await directory.HistoryAsync(currentUser.RequiredUserId, null, GetMyHistoryQuery.Limit, cancellationToken);
        return entries.Select(e => new MyHistoryEntryView(e.At, e.Action)).ToList();
    }
}

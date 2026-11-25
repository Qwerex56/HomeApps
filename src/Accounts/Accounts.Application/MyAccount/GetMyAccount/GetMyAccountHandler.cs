using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using FluentResults;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

internal sealed class GetMyAccountHandler(IUserRepository users, ICurrentUser currentUser) : IQueryHandler<GetMyAccountQuery, MyAccountView>
{
    public async Task<Result<MyAccountView>> Handle(GetMyAccountQuery query, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        return user is null ? Result.Fail<MyAccountView>(ApplicationErrors.UserPending) : user.ToMyView();
    }
}

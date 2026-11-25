using System.Security.Cryptography;
using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

internal sealed class GetUserAvatarHandler(IAvatarStore store) : IQueryHandler<GetUserAvatarQuery, AvatarFileView>
{
    public async Task<Result<AvatarFileView>> Handle(GetUserAvatarQuery query, CancellationToken cancellationToken)
    {
        var content = await store.ReadAsync(query.UserId, cancellationToken);
        var type = content is null ? null : AvatarImage.Sniff(content);
        if (content is null || type is null)
        {
            return Result.Fail<AvatarFileView>(UserErrors.AvatarNotFound);
        }

        var etag = $"\"{Convert.ToHexString(SHA256.HashData(content))[..32].ToLowerInvariant()}\"";
        return new AvatarFileView(content, type, etag);
    }
}

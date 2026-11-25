using Accounts.Application.Abstractions;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

internal sealed class UploadMyAvatarHandler(IUserRepository users, IAvatarStore store, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<UploadMyAvatarCommand, AvatarUploadedView>
{
    public async Task<Result<AvatarUploadedView>> Handle(UploadMyAvatarCommand command, CancellationToken cancellationToken)
    {
        if (command.Content.Length > AvatarImage.MaxBytes)
        {
            return Result.Fail<AvatarUploadedView>(UserErrors.AvatarTooLarge);
        }

        if (AvatarImage.Sniff(command.Content) is null)
        {
            return Result.Fail<AvatarUploadedView>(UserErrors.AvatarTypeInvalid);
        }

        var user = await users.GetByIdAsync(currentUser.RequiredUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail<AvatarUploadedView>(ApplicationErrors.UserPending);
        }

        var avatar = AvatarUrl.ForUpload(user.Id, time.GetUtcNow().ToUnixTimeMilliseconds());
        var updated = user.UpdateProfile(currentUser.Actor(), user.Profile.WithAvatar(avatar), time);
        if (updated.IsFailed)
        {
            return updated.ToResult<AvatarUploadedView>();
        }

        await store.SaveAsync(user.Id, command.Content, cancellationToken);
        return new AvatarUploadedView(avatar.Value);
    }
}

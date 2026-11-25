using Accounts.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

/// <summary>2.15: a user's uploaded avatar; open to anyone, like a profile picture on a page. 404 <c>profile.avatar_not_found</c> without one.</summary>
public sealed record GetUserAvatarQuery(Guid UserId) : IQuery<AvatarFileView>;

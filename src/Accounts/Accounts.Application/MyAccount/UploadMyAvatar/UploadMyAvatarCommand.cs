using Accounts.Application.Common;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Accounts.Application.MyAccount;

/// <summary>2.15: the caller's new avatar picture (JPEG, PNG or WebP, at most 2 MB).</summary>
public sealed record UploadMyAvatarCommand(byte[] Content) : ICommand<AvatarUploadedView>;

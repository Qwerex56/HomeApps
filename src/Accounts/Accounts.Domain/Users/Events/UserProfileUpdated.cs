using HomeApps.BuildingBlocks.Domain;

namespace Accounts.Domain.Users.Events;

/// <summary>
/// The name or avatar changed: the only profile data other services copy. Becomes
/// <c>HomeApps.Contracts.UserProfileUpdated</c>. Other profile changes only go to the history.
/// </summary>
public sealed record UserProfileUpdated(Guid UserId, string DisplayName, Uri? AvatarUrl) : IDomainEvent;

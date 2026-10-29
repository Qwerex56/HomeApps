using HomeApps.BuildingBlocks.Domain;

namespace Accounts.Domain.Users.Events;

/// <summary>
/// The account was disabled. <see cref="BySelf"/> tells Auth whether a correct sign-in may restore it (R8) or only a
/// higher role can. Becomes <c>HomeApps.Contracts.Accounts.UserDisabled</c>.
/// </summary>
public sealed record UserDisabled(Guid UserId, bool BySelf) : IDomainEvent;

/// <summary>The account is active again. Becomes <c>HomeApps.Contracts.Accounts.UserEnabled</c>.</summary>
public sealed record UserEnabled(Guid UserId) : IDomainEvent;

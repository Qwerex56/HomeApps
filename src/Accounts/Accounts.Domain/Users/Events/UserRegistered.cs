using HomeApps.BuildingBlocks.Domain;

namespace Accounts.Domain.Users.Events;

/// <summary>A user exists (UC-08, UC-09). Becomes <c>HomeApps.Contracts.UserCreated</c> for Household.</summary>
public sealed record UserRegistered(Guid UserId, string DisplayName, Uri? AvatarUrl) : IDomainEvent;

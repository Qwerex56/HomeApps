using HomeApps.BuildingBlocks.Domain;

namespace Accounts.Domain.Users.Events;

/// <summary>The "forget me" erase (R5). Becomes <c>HomeApps.Contracts.Accounts.UserErased</c>: Auth and Household delete their data.</summary>
public sealed record UserErased(Guid UserId) : IDomainEvent;

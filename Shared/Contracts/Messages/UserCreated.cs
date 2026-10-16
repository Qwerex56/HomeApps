using Shared.Authorization;

namespace Shared.Contracts.Messages;

public record UserCreated(
    Guid UserId,
    string Username,
    string Email,
    UserSystemRoleEnum Role);
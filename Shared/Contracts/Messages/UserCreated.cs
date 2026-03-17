using Shared.Authorization;

namespace Shared.Contracts.Messages;

public class UserCreated {
    public required Guid UserId { get; set; }
    
    public required string Username { get; set; } = string.Empty;
    public required string Email { get; set; } = string.Empty;
    public UserSystemRoleEnum Role { get; set; }
}
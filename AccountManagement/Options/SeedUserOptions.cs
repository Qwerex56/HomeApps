using Shared.Authorization;

namespace AccountManagement.Options;

public class SeedUserOptions {
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public UserSystemRoleEnum Role { get; set; }
}
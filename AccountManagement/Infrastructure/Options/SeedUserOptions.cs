using Shared.Authorization;

namespace AccountService.Infrastructure.Options;

public class SeedUserOptions {
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public UserSystemRoleEnum Role { get; set; }
}
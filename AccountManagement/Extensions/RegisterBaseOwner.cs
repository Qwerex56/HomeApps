using AccountManagement.Dto.User;
using AccountManagement.Options;
using AccountManagement.Services.UserService;
using Microsoft.Extensions.Options;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

namespace AccountManagement.Extensions;

public static class RegisterBaseOwner {
    public static async Task SeedUserAsync(WebApplication app) {
        using var scope = app.Services.CreateScope();
        
        var env = scope.ServiceProvider.GetRequiredService<IHostingEnvironment>();

        if (!env.IsDevelopment()) {
            return;
        }
        
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedUserOptions>>().Value;
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var dto = new CreateUserByAdminDto {
            Name = options.Name,
            Email = options.Email,
            Password = options.Password,
            Role = options.Role,
        };
        
        await userService.CreateUserWithPasswordAsync(dto);
    }
}
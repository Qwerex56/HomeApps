using AccountManagement.Models;
using AccountManagement.Repositories.ExternalCredentialRepository;
using AccountManagement.Repositories.HouseholdRepository;
using AccountManagement.Repositories.JwtRepository;
using AccountManagement.Repositories.UserCredentialRepository;
using AccountManagement.Repositories.UserHouseholdRepository;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Services.HouseholdService.HouseholdCommandService;
using AccountManagement.Services.HouseholdService.HouseholdQueryService;
using AccountManagement.Services.LoginService;
using AccountManagement.Services.UserService;
using AccountManagement.Workers.UnitOfWork;
using Microsoft.AspNetCore.Identity;

namespace AccountManagement.Extensions;

public static class RegisterServices {
    public static void AddRepositories(this IServiceCollection services) {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHouseHoldRepository, HouseholdRepository>();
        services.AddScoped<IUserHouseHoldRepository, UserHouseholdRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
        services.AddScoped<IExternalCredentialRepository, ExternalCredentialRepository>();
    }

    public static void AddUnitOfWorks(this IServiceCollection services) {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
    
    public static void AddAppServices(this IServiceCollection services) {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILoginService, LoginService>();
        
        services.AddScoped<IHouseholdQueryService, HouseholdQueryService>();
        services.AddScoped<IHouseholdCommandService, HouseholdCommandService>();
        
        services.AddScoped<IPasswordHasher<User>,  PasswordHasher<User>>();
    }
}
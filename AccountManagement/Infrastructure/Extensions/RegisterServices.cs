using AccountService.Application.Services.CookieService;
using AccountService.Application.Services.LoginService;
using AccountService.Application.Services.UserService;
using AccountService.Domain.Models;
using AccountService.Infrastructure.Repositories.ExternalCredentialRepository;
using AccountService.Infrastructure.Repositories.RefreshTokenRepository;
using AccountService.Infrastructure.Repositories.UserCredentialRepository;
using AccountService.Infrastructure.Repositories.UserRepository;
using AccountService.Infrastructure.Workers.UnitOfWork;
using Microsoft.AspNetCore.Identity;

namespace AccountService.Infrastructure.Extensions;

public static class RegisterServices {
    public static void AddRepositories(this IServiceCollection services) {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
        services.AddScoped<IExternalCredentialRepository, ExternalCredentialRepository>();
    }

    public static void AddUnitOfWorks(this IServiceCollection services) {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
    
    public static void AddAppServices(this IServiceCollection services) {
        services.AddScoped<ICookieService, CookieService>();
        
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILoginService, LoginService>();
        
        services.AddScoped<IPasswordHasher<User>,  PasswordHasher<User>>();
    }
}
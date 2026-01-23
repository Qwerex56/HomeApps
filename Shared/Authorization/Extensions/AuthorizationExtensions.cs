using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Authorization.Extensions;

public static class AuthorizationExtensions {
    public static void AddAllCustomPolicies(this AuthorizationOptions options) {
        options.AddSystemAdminPolicy();
        options.AddSystemOwnerPolicy();
        options.AddSystemMemberPolicy();
    }

    public static void AddSystemOwnerPolicy(this AuthorizationOptions options) {
        options.AddPolicy(Policies.RequireSystemOwner,
            policy => policy.RequireRole(nameof(UserSystemRoleEnum.SystemOwner)));
    }

    public static void AddSystemAdminPolicy(this AuthorizationOptions options) {
        options.AddPolicy(Policies.RequireSystemAdmin,
            policy => policy.RequireRole(nameof(UserSystemRoleEnum.SystemAdmin)));
    }

    public static void AddSystemMemberPolicy(this AuthorizationOptions options) {
        options.AddPolicy(Policies.RequireSystemMember,
            policy => policy.RequireRole(nameof(UserSystemRoleEnum.SystemMember)));
    }
    
    // --- ADD JWT BEARER ---
    public static void AddJwtBearerAuthentication(this IServiceCollection services, IConfiguration config) {
        var jwtConfig = config.GetSection("JwtSettings");
        var jwtKey = Encoding.UTF8.GetBytes(jwtConfig["SecretKey"]);
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidIssuer = jwtConfig["Issuer"],

                ValidateAudience = true,
                ValidAudience = jwtConfig["Audience"],

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            };
        });
    }
}
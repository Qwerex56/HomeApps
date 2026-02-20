using AccountManagement.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccountManagement.Extensions;

public static class CorsExtensions {
    public const string FrontendCorsPolicy = "FrontendCorsPolicy";

    public static void AddFrontendCors(this IServiceCollection services, IConfiguration configuration, ILogger logger) {
        services.Configure<FrontendCorsOptions>(configuration.GetSection("FrontendCorsOptions"));

        var frontendDomain = configuration.GetSection("FrontendCorsOptions")
                                          .Get<FrontendCorsOptions>()?.FrontendDomain
                             ?? string.Empty;
        logger.LogInformation("[CORS] FrontendDomain = '{FrontendDomain}'", frontendDomain);

        if (string.IsNullOrWhiteSpace(frontendDomain)) {
            throw new InvalidOperationException(
                "CORS configuration is invalid: 'FrontendCorsOptions:FrontendDomain' must be set to a valid URL.");
        }

        services.AddCors(options => {
            options.AddPolicy(FrontendCorsPolicy, policy => {
                policy.WithOrigins(frontendDomain)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });
    }
}

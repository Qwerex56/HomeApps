using System.Reflection;
using HomeApps.BuildingBlocks.Api.Auth;
using HomeApps.BuildingBlocks.Api.Endpoints;
using HomeApps.BuildingBlocks.Api.Errors;
using HomeApps.BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HomeApps.BuildingBlocks.Api;

public static class HomeAppsApiExtensions
{
    /// <summary>Where Docker Compose mounts secrets.</summary>
    public const string SecretsDirectory = "/run/secrets";

    /// <summary>
    /// Everything a HomeApps API needs besides its own use cases:
    /// ProblemDetails + last-resort exception handler, JWT bearer validated against the Auth service's JWKS,
    /// authentication required by default (anonymous is opt-in), <see cref="ICurrentUser"/>, health checks,
    /// OpenAPI and endpoint discovery from <paramref name="endpointsAssembly"/>. Docker secrets in
    /// <see cref="SecretsDirectory"/> are read as settings, one file per key (<c>Auth__Signing__PrivateKeyPem</c>),
    /// for values that do not fit an environment variable, such as a PEM key.
    /// </summary>
    public static WebApplicationBuilder AddHomeAppsDefaults(this WebApplicationBuilder builder, Assembly endpointsAssembly)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Configuration.AddKeyPerFile(SecretsDirectory, optional: true);
        var services = builder.Services;

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier));
        services.AddExceptionHandler<LastResortExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddHomeAppsJwtBearer();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        services.AddEndpoints(endpointsAssembly);

        return builder;
    }

    /// <summary>
    /// The matching middleware pipeline. Health checks are anonymous: <c>/health/live</c> (process up) and
    /// <c>/health/ready</c> (dependencies such as the database). OpenAPI is served at <c>/openapi/v1.json</c> in Development.
    /// </summary>
    /// <param name="endpointsPrefix">Maps the <see cref="IEndpoint"/>s under this prefix, e.g. <c>/api</c> for a plugin.</param>
    public static WebApplication UseHomeAppsDefaults(this WebApplication app, string? endpointsPrefix = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live") }).AllowAnonymous();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        app.MapEndpoints(endpointsPrefix is null ? null : app.MapGroup(endpointsPrefix));

        return app;
    }

    private static void AddHomeAppsJwtBearer(this IServiceCollection services)
    {
        services
            .AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.Authority = jwt.Authority;
                bearer.Audience = jwt.Audience;
                bearer.RequireHttpsMetadata = jwt.RequireHttpsMetadata;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = string.IsNullOrWhiteSpace(jwt.Issuer) ? jwt.Authority : jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimNames.Subject,
                    RoleClaimType = ClaimNames.Role,
                };
            });
    }
}

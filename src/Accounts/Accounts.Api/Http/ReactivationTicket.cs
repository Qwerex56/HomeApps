using HomeApps.BuildingBlocks.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Accounts.Api.Http;

/// <summary>
/// UC-05: Auth hands a self-disabled user a short ticket instead of tokens (<c>typ = ticket+jwt</c>,
/// <c>aud = homeapps-ticket</c>, <c>scope = reactivation</c>), signed with the same keys as access tokens. A separate
/// scheme accepts only that ticket, and only the two reactivation endpoints use it. Access tokens fail here (other
/// <c>aud</c>) and the ticket fails everywhere else (the default scheme expects <c>aud = homeapps</c>).
/// </summary>
internal static class ReactivationTicket
{
    public const string Scheme = "ReactivationTicket";
    public const string Policy = "reactivation-ticket";
    public const string Audience = "homeapps-ticket";
    public const string TokenType = "ticket+jwt";
    public const string ScopeClaim = "scope";
    public const string Scope = "reactivation";

    public static IServiceCollection AddReactivationTicket(this IServiceCollection services)
    {
        new AuthenticationBuilder(services).AddJwtBearer(Scheme);

        services.AddOptions<JwtBearerOptions>(Scheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.Authority = jwt.Authority;
                bearer.RequireHttpsMetadata = jwt.RequireHttpsMetadata;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = string.IsNullOrWhiteSpace(jwt.Issuer) ? jwt.Authority : jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidTypes = [TokenType],
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimNames.Subject,
                    RoleClaimType = ClaimNames.Role,
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.FindFirst(ScopeClaim)?.Value != Scope)
                        {
                            context.Fail("Not a reactivation ticket.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser());

        return services;
    }
}

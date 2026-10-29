using System.Text.Json.Serialization;
using Auth.Api.Http;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Auth.Api;

internal static class DependencyInjection
{
    /// <summary>
    /// What only the Auth Api needs on top of the kit: its own keys for validating its own tokens, the encrypted
    /// Google flow cookie (Q13), rate limits (Q17), the client IP from Traefik and enums as strings in JSON.
    /// </summary>
    public static IServiceCollection AddAuthApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Auth validates the access tokens it signed itself with the keys it holds; other services fetch the JWKS.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<SigningKeys, IOptions<SigningOptions>>((bearer, keys, signing) =>
            {
                var own = new OpenIdConnectConfiguration { Issuer = signing.Value.Issuer };
                foreach (var key in keys.ValidationKeys)
                {
                    own.SigningKeys.Add(key);
                }

                bearer.Configuration = own;
            });

        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        var keysPath = configuration["Auth:DataProtection:KeysPath"] ?? ".keys/dataprotection";
        services.AddDataProtection()
            .SetApplicationName("homeapps-auth")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(environment.ContentRootPath, keysPath)));
        services.AddSingleton<ExternalFlows>();

        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            var networks = configuration.GetSection("Auth:Proxy:KnownNetworks").Get<string[]>() ?? [];
            if (networks.Length > 0)
            {
                forwarded.KnownIPNetworks.Clear();
                forwarded.KnownProxies.Clear();
                foreach (var network in networks)
                {
                    forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                }
            }
        });

        services.AddAuthRateLimits();

        return services;
    }
}

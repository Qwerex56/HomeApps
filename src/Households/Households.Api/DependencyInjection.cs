using System.Text.Json.Serialization;
using Households.Api.Http;
using Households.Application.Common;

namespace Households.Api;

internal static class DependencyInjection
{
    /// <summary>
    /// What only the Households Api needs on top of the kit: <see cref="HouseholdSettings"/> from configuration (D10),
    /// the admin policy (D8, Q7), the limits (Q8) and enums as strings in JSON. Called before
    /// <c>AddHouseholdsApplication</c>, so its defaults do not replace the bound settings.
    /// </summary>
    public static IServiceCollection AddHouseholdsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddSingleton(configuration.GetSection(HouseholdSettings.Section).Get<HouseholdSettings>() ?? new HouseholdSettings());

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => policy.RequireRole("Admin", "Owner"));

        services.AddHouseholdsRateLimits();

        return services;
    }
}

internal static class Policies
{
    /// <summary>The <c>/admin</c> group: server role Admin or Owner (D8).</summary>
    public const string Admin = "households-admin";
}

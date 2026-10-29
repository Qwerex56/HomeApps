using System.Text.Json.Serialization;
using Accounts.Api.Http;

namespace Accounts.Api;

internal static class DependencyInjection
{
    /// <summary>What only the Accounts Api needs on top of the kit: the reactivation ticket, the admin policy, the limit
    /// on changes (Q6, Q9) and enums as strings in JSON.</summary>
    public static IServiceCollection AddAccountsApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddReactivationTicket();
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => policy.RequireRole("Admin", "Owner"));

        services.AddAccountsRateLimits();

        return services;
    }
}

internal static class Policies
{
    /// <summary>The <c>/admin</c> group: role Admin or Owner, so a plain user gets 403 before any query runs.</summary>
    public const string Admin = "accounts-admin";
}

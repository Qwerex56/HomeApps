using Accounts.Application.Abstractions;
using Accounts.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounts.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Q7: an Auth event that came before <c>IdentityRegistered</c> is tried again after 1, 2, 5, 10 and 30 seconds
    /// (about 48 s in all), then it waits in the <c>_error</c> queue to be moved back by hand.
    /// </summary>
    public static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
    ];

    /// <summary>
    /// The adapters behind Application's ports: Postgres with the outbox, the read queries and the consumers of Auth's
    /// events. The Api calls <c>AddAccountsApplication</c> next to it.
    /// </summary>
    public static IServiceCollection AddAccountsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHomeAppsInfrastructure<AccountsDbContext>(
            configuration, "accounts", typeof(DependencyInjection).Assembly, retry => retry.Intervals(RetryIntervals));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddSingleton<IAvatarStore>(new FileAvatarStore(configuration["Avatars:Path"] ?? FileAvatarStore.DefaultRoot));
        services.AddSingleton<IServerInfo>(_ =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("HomeApps");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            var plugins = (configuration["ServerAbout:Plugins"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new About.ServerInfo(http, plugins);
        });
        services.AddHostedService<Jobs.ProfileRepublishOnStart>();

        return services;
    }
}

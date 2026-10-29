using Households.Application.Abstractions;
using Households.Infrastructure.Jobs;
using Households.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Households.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Q5 (as Accounts Q7): an Accounts event that came before <c>UserCreated</c> is tried again after 1, 2, 5, 10 and
    /// 30 seconds (about 48 s in all), then it waits in the <c>_error</c> queue to be moved back by hand.
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
    /// The adapters behind Application's ports: Postgres with the outbox, the read queries, the consumers of Accounts'
    /// events and the hourly guest clean-up (D2c). The Api calls <c>AddHouseholdsApplication</c> next to it.
    /// </summary>
    public static IServiceCollection AddHouseholdsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHomeAppsInfrastructure<HouseholdsDbContext>(
            configuration, "households", typeof(DependencyInjection).Assembly, retry => retry.Intervals(RetryIntervals));

        services.AddScoped<IHouseholdRepository, HouseholdRepository>();
        services.AddScoped<IInviteRepository, InviteRepository>();
        services.AddScoped<IUserCopies, UserCopies>();
        services.AddScoped<IHouseholdDirectory, HouseholdDirectory>();
        services.AddHostedService<ExpiredGuestsCleanup>();

        return services;
    }
}

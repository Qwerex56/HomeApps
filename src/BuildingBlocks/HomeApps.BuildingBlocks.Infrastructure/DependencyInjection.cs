using System.Reflection;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Infrastructure.Messaging;
using HomeApps.BuildingBlocks.Infrastructure.Options;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using HomeApps.BuildingBlocks.Infrastructure.Time;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeApps.BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Name of the connection string every service uses for its own database.</summary>
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// Wires the service's infrastructure in one call:
    /// <list type="bullet">
    /// <item>Postgres DbContext with retry on transient failures, registered as <see cref="IUnitOfWork"/>, plus a health check.</item>
    /// <item>MassTransit on RabbitMQ with the EF Core outbox (bus outbox for publishing, inbox on every consumer endpoint).</item>
    /// <item>Consumers from <paramref name="consumersAssembly"/>, <see cref="IEventBus"/> and <see cref="IDateTimeProvider"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="serviceName">Short service name, used as the queue name prefix (e.g. <c>accounts</c>).</param>
    public static IServiceCollection AddHomeAppsInfrastructure<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Assembly consumersAssembly)
        where TContext : HomeAppsDbContext
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddHomeAppsPersistence<TContext>(configuration);
        services.AddHomeAppsMessaging<TContext>(configuration, serviceName, consumersAssembly);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }

    public static IServiceCollection AddHomeAppsPersistence<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : HomeAppsDbContext
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        services.AddDbContext<TContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());

        services.AddHealthChecks().AddDbContextCheck<TContext>("database", tags: ["ready"]);

        return services;
    }

    public static IServiceCollection AddHomeAppsMessaging<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Assembly consumersAssembly)
        where TContext : HomeAppsDbContext
    {
        services.AddValidatedOptions<RabbitMqOptions>(RabbitMqOptions.SectionName);
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(bus =>
        {
            bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: serviceName, includeNamespace: false));
            bus.AddConsumers(consumersAssembly);

            bus.AddEntityFrameworkOutbox<TContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            // Inbox + outbox on every consumer: duplicates are ignored, and messages a consumer publishes
            // are committed together with its database changes.
            bus.AddConfigureEndpointsCallback((context, _, endpoint) => endpoint.UseEntityFrameworkOutbox<TContext>(context));

            bus.UsingRabbitMq((context, rabbit) =>
            {
                var options = configuration.GetValidatedOptions<RabbitMqOptions>(RabbitMqOptions.SectionName);
                rabbit.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });
                rabbit.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

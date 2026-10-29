using System.Reflection;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Infrastructure.Messaging;
using HomeApps.BuildingBlocks.Infrastructure.Options;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
    /// <item>Consumers from <paramref name="consumersAssembly"/>, <see cref="IEventBus"/> and the .NET <see cref="TimeProvider"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="serviceName">Short service name, used as the queue name prefix (e.g. <c>accounts</c>).</param>
    /// <param name="retry">Optional retry for every consumer endpoint, applied before the inbox and outbox so each
    /// attempt gets a fresh transaction. Without it a failed message goes straight to the <c>_error</c> queue.</param>
    public static IServiceCollection AddHomeAppsInfrastructure<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Assembly consumersAssembly,
        Action<IRetryConfigurator>? retry = null)
        where TContext : HomeAppsDbContext =>
        services.AddHomeAppsInfrastructure<TContext>(configuration, serviceName, [consumersAssembly], retry);

    /// <summary>The same with consumers from several assemblies, e.g. a plugin's own and the plugin kit's.</summary>
    public static IServiceCollection AddHomeAppsInfrastructure<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Assembly[] consumersAssemblies,
        Action<IRetryConfigurator>? retry = null)
        where TContext : HomeAppsDbContext
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.AddHomeAppsPersistence<TContext>(configuration);
        services.AddHomeAppsMessaging<TContext>(configuration, serviceName, consumersAssemblies, retry);

        services.TryAddSingleton(TimeProvider.System);

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

    /// <summary>
    /// MassTransit with the EF Core outbox. The transport comes from <c>Messaging:Transport</c>:
    /// <c>RabbitMq</c> (default) or <c>InMemory</c>, which runs MassTransit's in-memory test harness
    /// (integration tests, or running one service alone; never production).
    /// </summary>
    public static IServiceCollection AddHomeAppsMessaging<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Assembly consumersAssembly,
        Action<IRetryConfigurator>? retry = null)
        where TContext : HomeAppsDbContext =>
        services.AddHomeAppsMessaging<TContext>(configuration, serviceName, [consumersAssembly], retry);

    public static IServiceCollection AddHomeAppsMessaging<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Assembly[] consumersAssemblies,
        Action<IRetryConfigurator>? retry = null)
        where TContext : HomeAppsDbContext
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<IEventBus, MassTransitEventBus>();

        var transport = configuration[MessagingTransportKey] ?? RabbitMqTransport;
        if (string.Equals(transport, InMemoryTransport, StringComparison.OrdinalIgnoreCase))
        {
            services.AddMassTransitTestHarness(bus => ConfigureBus<TContext>(bus, serviceName, consumersAssemblies, retry));
            return services;
        }

        if (!string.Equals(transport, RabbitMqTransport, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unknown {MessagingTransportKey} '{transport}'. Use '{RabbitMqTransport}' or '{InMemoryTransport}'.");
        }

        services.AddValidatedOptions<RabbitMqOptions>(RabbitMqOptions.SectionName);
        services.AddMassTransit(bus =>
        {
            ConfigureBus<TContext>(bus, serviceName, consumersAssemblies, retry);

            bus.UsingRabbitMq((context, rabbit) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
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

    public const string MessagingTransportKey = "Messaging:Transport";
    public const string RabbitMqTransport = "RabbitMq";
    public const string InMemoryTransport = "InMemory";

    private static void ConfigureBus<TContext>(
        IBusRegistrationConfigurator bus, string serviceName, Assembly[] consumersAssemblies, Action<IRetryConfigurator>? retry)
        where TContext : HomeAppsDbContext
    {
        bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: serviceName, includeNamespace: false));
        bus.AddConsumers(consumersAssemblies);

        bus.AddEntityFrameworkOutbox<TContext>(outbox =>
        {
            outbox.UsePostgres();
            outbox.UseBusOutbox();
            outbox.QueryDelay = TimeSpan.FromSeconds(1);
        });

        // Inbox + outbox on every consumer: duplicates are ignored, and messages a consumer publishes
        // are committed together with its database changes.
        bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
        {
            if (retry is not null)
            {
                endpoint.UseMessageRetry(retry);
            }

            endpoint.UseEntityFrameworkOutbox<TContext>(context);
        });
    }
}

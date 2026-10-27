using Accounts.Application;
using Accounts.Application.Abstractions;
using Accounts.Infrastructure.Auth;
using HomeApps.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Accounts.Infrastructure.Tests;

public sealed class WiringTests
{
    [Fact]
    public async Task Every_handler_port_and_consumer_resolves()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=accounts_wiring",
            ["Messaging:Transport"] = "InMemory",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICurrentUser>());
        services.AddAccountsApplication();
        services.AddAccountsInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();

        var handlerTypes = typeof(Application.DependencyInjection).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Distinct();
        Assert.All(handlerTypes, t => Assert.NotEmpty(scope.ServiceProvider.GetServices(t)));

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserDirectory>());
        Assert.NotNull(scope.ServiceProvider.GetService<IdentityRegisteredConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<IdentityEmailChangedConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<IdentityRoleChangedConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<IdentitySignedInConsumer>());
    }
}

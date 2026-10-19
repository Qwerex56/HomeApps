using Auth.Application.Abstractions;
using Auth.Application.Logout;
using Auth.Domain.Common;
using HomeApps.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Auth.Application.Tests;

public sealed class WiringTests
{
    private static ServiceProvider Build(World w)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:RegistrationOpen"] = "true",
                ["Auth:Bootstrap:Email"] = "owner@example.com",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddAuthApplication(configuration);
        services.AddSingleton<IIdentityRepository>(w.Identities);
        services.AddSingleton<IInvitationRepository>(w.Invitations);
        services.AddSingleton<IClientRegistry>(w.Clients);
        services.AddSingleton<ITokenIssuer>(w.Tokens);
        services.AddSingleton<IExternalIdentityVerifier>(w.Verifier);
        services.AddSingleton<IPasswordHasher>(w.Hasher);
        services.AddSingleton<IUnitOfWork>(w.UnitOfWork);
        services.AddSingleton<ICurrentUser>(w.CurrentUser);
        services.AddSingleton<IEventBus>(w.Bus);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void Options_bind_from_the_auth_section()
    {
        using var provider = Build(new World());

        var options = provider.GetRequiredService<IOptions<AuthOptions>>().Value;

        Assert.True(options.RegistrationOpen);
        Assert.Equal("owner@example.com", options.Bootstrap.Email);
    }

    [Fact]
    public async Task Every_handler_resolves_and_the_pipeline_validates_and_saves()
    {
        var w = new World();
        using var provider = Build(w);
        using var scope = provider.CreateScope();

        var handlerTypes = typeof(AuthOptions).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Distinct()
            .ToList();
        Assert.NotEmpty(handlerTypes);
        Assert.All(handlerTypes, t => Assert.NotEmpty(scope.ServiceProvider.GetServices(t)));

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var invalid = await sender.Send(new SignOutCommand(""));
        Assert.True(invalid.IsFailed);
        Assert.Equal(0, w.UnitOfWork.Saves);

        Assert.True((await sender.Send(new SignOutCommand("unknown"))).IsSuccess);
        Assert.Equal(1, w.UnitOfWork.Saves);
    }
}

using Accounts.Application.Abstractions;
using Accounts.Application.MyAccount;
using HomeApps.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Accounts.Application.Tests;

public sealed class WiringTests
{
    private static ServiceProvider Build(World w, FakeUnitOfWork unitOfWork)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddAccountsApplication();
        services.AddSingleton<IUserRepository>(w.Users);
        services.AddSingleton<IUserDirectory>(w.Users);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<ICurrentUser>(w.CurrentUser);
        services.AddSingleton<IEventBus>(w.Bus);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public async Task Every_handler_resolves_and_the_pipeline_validates_and_saves()
    {
        var w = new World();
        var unitOfWork = new FakeUnitOfWork();
        using var provider = Build(w, unitOfWork);
        using var scope = provider.CreateScope();

        var handlerTypes = typeof(DependencyInjection).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Distinct()
            .ToList();
        Assert.NotEmpty(handlerTypes);
        Assert.All(handlerTypes, t => Assert.NotEmpty(scope.ServiceProvider.GetServices(t)));

        var user = await w.Registered();
        w.SignInAs(user);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var invalid = await sender.Send(new UpdateMyProfileCommand(Forms.Profile() with { DisplayName = "" }));
        Assert.True(invalid.IsFailed);
        Assert.Equal(0, unitOfWork.Saves);

        Assert.True((await sender.Send(new UpdateMyProfileCommand(Forms.Profile("Adam N.")))).IsSuccess);
        Assert.Equal(1, unitOfWork.Saves);
    }
}

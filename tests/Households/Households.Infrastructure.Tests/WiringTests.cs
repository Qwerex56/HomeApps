using HomeApps.BuildingBlocks.Application.Abstractions;
using Households.Application;
using Households.Application.Abstractions;
using Households.Domain.Households;
using Households.Infrastructure.Accounts;
using Households.Infrastructure.Jobs;
using Households.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Households.Infrastructure.Tests;

public sealed class WiringTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private ServiceProvider Build(TimeProvider? time = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = database.ConnectionString,
            ["Messaging:Transport"] = "InMemory",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICurrentUser>());
        if (time is not null)
        {
            services.AddSingleton(time);
        }

        services.AddHouseholdsApplication();
        services.AddHouseholdsInfrastructure(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task Every_handler_port_and_consumer_resolves()
    {
        await using var provider = Build();
        await using var scope = provider.CreateAsyncScope();

        var handlerTypes = typeof(Application.DependencyInjection).Assembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .Distinct();
        Assert.All(handlerTypes, t => Assert.NotEmpty(scope.ServiceProvider.GetServices(t)));

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHouseholdRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInviteRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserCopies>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IHouseholdDirectory>());
        Assert.NotNull(scope.ServiceProvider.GetService<UserCreatedConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<UserProfileUpdatedConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<UserDisabledConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<UserEnabledConsumer>());
        Assert.NotNull(scope.ServiceProvider.GetService<UserErasedConsumer>());
        Assert.Single(provider.GetServices<IHostedService>().OfType<ExpiredGuestsCleanup>());
    }

    [Fact]
    public async Task Cleanup_removes_guests_whose_access_ended()
    {
        var time = new FixedTime(A.Now);
        var (household, owner, _, invite) = A.Household(time);
        var code = household.RegenerateGuestLink(A.User(owner), time).Value;
        var guest = Guid.NewGuid();
        Assert.True(household.JoinAsGuest(guest, code, 100, time).IsSuccess);
        Assert.True(household.SetGuestAccess(A.User(owner), guest, A.Now.AddHours(1), time).IsSuccess);
        household.ClearDomainEvents();
        await using (var db = database.CreateContext())
        {
            db.Households.Add(household);
            db.Invites.Add(invite);
            await db.SaveChangesAsync();
        }

        time.Now = A.Now.AddHours(2);
        await using var provider = Build(time);
        await provider.GetServices<IHostedService>().OfType<ExpiredGuestsCleanup>().Single().RunOnceAsync(default);

        await using var read = database.CreateContext();
        var members = await read.Memberships.Where(m => m.HouseholdId == household.Id).Select(m => m.UserId).ToListAsync();
        Assert.DoesNotContain(guest, members);
        Assert.Equal(2, members.Count);
    }
}

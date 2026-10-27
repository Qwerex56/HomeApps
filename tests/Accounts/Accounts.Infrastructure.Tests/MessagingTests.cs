using Accounts.Application;
using Accounts.Application.AuthSync;
using Accounts.Application.Common;
using Accounts.Domain.Users;
using Accounts.Infrastructure.Auth;
using Accounts.Infrastructure.Persistence;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Auth;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Accounts.Infrastructure.Tests;

public sealed class MessagingTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private static ConsumeContext<T> Context<T>(T message)
        where T : class
    {
        var context = Substitute.For<ConsumeContext<T>>();
        context.Message.Returns(message);
        return context;
    }

    private static IdentityRegistered Identity(Guid id, string email) =>
        new(id, email, HomeApps.Contracts.Auth.ServerRole.User, "Adam", "Adam", "Nowak", null,
            HomeApps.Contracts.Auth.RegistrationSource.Password, A.Now, false, null, null,
            new RegistrationContext("homeapps-web", "Firefox", "pl-PL"));

    private static ISender Answering(Result result)
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result>>(), Arg.Any<CancellationToken>()).Returns(result);
        return sender;
    }

    [Fact]
    public async Task Each_consumer_sends_its_command()
    {
        var sender = Answering(Result.Ok());
        var id = Guid.NewGuid();
        var identity = Identity(id, "adam@example.com");

        await new IdentityRegisteredConsumer(sender).Consume(Context(identity));
        await new IdentityEmailChangedConsumer(sender).Consume(Context(new IdentityEmailChanged(id, "nowy@example.com")));
        await new IdentityRoleChangedConsumer(sender).Consume(Context(new IdentityRoleChanged(id, HomeApps.Contracts.Auth.ServerRole.Admin)));
        await new IdentitySignedInConsumer(sender).Consume(Context(new IdentitySignedIn(id, A.Now, SignInMethod.Password, "homeapps-web")));

        await sender.Received(1).Send(new RegisterUserCommand(identity), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new SyncEmailCommand(id, "nowy@example.com"), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new SyncRoleCommand(id, HomeApps.Contracts.Auth.ServerRole.Admin), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new RecordSignInCommand(id, A.Now), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pending_user_throws_so_the_message_is_retried()
    {
        var consumer = new IdentityEmailChangedConsumer(Answering(Result.Fail(ApplicationErrors.UserPending)));

        await Assert.ThrowsAsync<UserNotYetRegisteredException>(
            () => consumer.Consume(Context(new IdentityEmailChanged(Guid.NewGuid(), "a@example.com"))));
    }

    [Fact]
    public async Task Any_other_failure_throws_too()
    {
        var consumer = new IdentityEmailChangedConsumer(Answering(Result.Fail(new AppError("email.invalid", "Bad.", ErrorKind.Validation))));

        var error = await Assert.ThrowsAsync<AuthEventRejectedException>(
            () => consumer.Consume(Context(new IdentityEmailChanged(Guid.NewGuid(), "zły"))));
        Assert.Contains("Bad.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Event_before_registration_is_applied_once_the_user_exists()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = database.ConnectionString,
            ["Messaging:Transport"] = "InMemory",
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICurrentUser>());
        services.AddAccountsApplication();
        services.AddAccountsInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(20);
        await harness.Start();

        var id = Guid.NewGuid();
        var email = A.Unique("adam");
        await harness.Bus.Publish(new IdentityEmailChanged(id, $"nowy.{email}"));
        await Task.Delay(300);
        await harness.Bus.Publish(Identity(id, email));

        await WaitFor(async () =>
        {
            await using var db = database.CreateContext();
            return await db.Users.AnyAsync(u => u.Id == id && u.Email == Domain.Common.Email.Create($"nowy.{email}").Value);
        });

        await using var read = database.CreateContext();
        var actions = await read.History.Where(h => h.UserId == id).OrderBy(h => h.At).Select(h => h.Action).ToListAsync();
        Assert.Equal([HistoryAction.Registered, HistoryAction.EmailSynced], actions);
        await harness.Stop();
    }

    private static async Task WaitFor(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            await Task.Delay(200);
        }
    }
}

using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Domain.Errors;
using HomeApps.Contracts.Accounts;
using Households.Application;
using Households.Application.AccountsSync;
using Households.Application.Common;
using Households.Infrastructure.Accounts;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Households.Infrastructure.Tests;

public sealed class MessagingTests(PostgresFixture database) : IClassFixture<PostgresFixture>
{
    private static ConsumeContext<T> Context<T>(T message)
        where T : class
    {
        var context = Substitute.For<ConsumeContext<T>>();
        context.Message.Returns(message);
        return context;
    }

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

        await new UserCreatedConsumer(sender).Consume(Context(new UserCreated(id, "Adam", null)));
        await new UserProfileUpdatedConsumer(sender).Consume(Context(new UserProfileUpdated(id, "Adaś", "https://a/b.png")));
        await new UserDisabledConsumer(sender).Consume(Context(new UserDisabled(id, false)));
        await new UserEnabledConsumer(sender).Consume(Context(new UserEnabled(id)));
        await new UserErasedConsumer(sender).Consume(Context(new UserErased(id)));

        await sender.Received(1).Send(new CreateUserCopyCommand(id, "Adam", null), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new UpdateUserCopyCommand(id, "Adaś", "https://a/b.png"), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new DisableUserCopyCommand(id, false), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new EnableUserCopyCommand(id), Arg.Any<CancellationToken>());
        await sender.Received(1).Send(new ForgetUserCommand(id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unknown_user_throws_so_the_message_is_retried()
    {
        var consumer = new UserEnabledConsumer(Answering(Result.Fail(ApplicationErrors.UserPending)));

        await Assert.ThrowsAsync<UserNotYetKnownException>(() => consumer.Consume(Context(new UserEnabled(Guid.NewGuid()))));
    }

    [Fact]
    public async Task Any_other_failure_throws_too()
    {
        var consumer = new UserEnabledConsumer(Answering(Result.Fail(new AppError("x.bad", "Bad.", ErrorKind.Validation))));

        var error = await Assert.ThrowsAsync<AccountsEventRejectedException>(() => consumer.Consume(Context(new UserEnabled(Guid.NewGuid()))));
        Assert.Contains("Bad.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Event_before_UserCreated_is_applied_once_the_copy_exists()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = database.ConnectionString,
            ["Messaging:Transport"] = "InMemory",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICurrentUser>());
        services.AddHouseholdsApplication();
        services.AddHouseholdsInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(20);
        await harness.Start();

        var id = Guid.NewGuid();
        await harness.Bus.Publish(new UserProfileUpdated(id, "Adaś", null));
        await Task.Delay(300);
        await harness.Bus.Publish(new UserCreated(id, "Adam", null));

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            await using var db = database.CreateContext();
            if (await db.Users.AnyAsync(u => u.Id == id && u.DisplayName == "Adaś"))
            {
                break;
            }

            Assert.True(DateTime.UtcNow < deadline, "The profile update was not applied in time.");
            await Task.Delay(200);
        }

        await harness.Stop();
    }
}

using HomeApps.Contracts.Accounts;
using Jarvis.Application.Accounts.ForgetUser;
using MassTransit;
using MediatR;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>"Forget me" (jarvis-v1.md 6): the person's runs and calls go.</summary>
public sealed class UserErasedConsumer(ISender sender) : IConsumer<UserErased>
{
    public async Task Consume(ConsumeContext<UserErased> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await sender.Send(new ForgetUserCommand(context.Message.UserId), context.CancellationToken);
    }
}

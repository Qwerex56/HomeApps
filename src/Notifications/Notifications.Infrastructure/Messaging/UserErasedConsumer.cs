using HomeApps.Contracts.Accounts;
using MassTransit;
using MediatR;
using Notifications.Application.AccountsSync.EraseRecipient;

namespace Notifications.Infrastructure.Messaging;

/// <summary>"Forget me": everything Notifications kept about the person goes.</summary>
public sealed class UserErasedConsumer(ISender sender) : IConsumer<UserErased>
{
    public async Task Consume(ConsumeContext<UserErased> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await sender.Send(new EraseRecipientCommand(context.Message.UserId), context.CancellationToken);
    }
}

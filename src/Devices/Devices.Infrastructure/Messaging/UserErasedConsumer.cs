using Devices.Application.AccountsSync.EraseOwner;
using HomeApps.Contracts.Accounts;
using MassTransit;
using MediatR;

namespace Devices.Infrastructure.Messaging;

/// <summary>"Forget me": the person's devices go.</summary>
public sealed class UserErasedConsumer(ISender sender) : IConsumer<UserErased>
{
    public async Task Consume(ConsumeContext<UserErased> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await sender.Send(new EraseOwnerCommand(context.Message.UserId), context.CancellationToken);
    }
}

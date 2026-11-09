using HomeApps.Contracts.Accounts;
using MassTransit;
using MediatR;
using Notifications.Application.AccountsSync.ForgetDevices;

namespace Notifications.Infrastructure.Messaging;

/// <summary>A disabled account gets no notifications; its devices are forgotten.</summary>
public sealed class UserDisabledConsumer(ISender sender) : IConsumer<UserDisabled>
{
    public async Task Consume(ConsumeContext<UserDisabled> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await sender.Send(new ForgetDevicesCommand(context.Message.UserId), context.CancellationToken);
    }
}

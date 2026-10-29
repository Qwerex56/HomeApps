using Auth.Application.AccountsSync;
using Auth.Domain.Identities;
using HomeApps.Contracts.Accounts;
using MassTransit;
using MediatR;

namespace Auth.Infrastructure.Accounts;

/// <summary>
/// A-32: Accounts owns the account state; Auth keeps a copy to decide who may sign in. Each consumer only sends the
/// command. The kit's inbox drops duplicates; the commands are idempotent anyway.
/// </summary>
public sealed class UserDisabledConsumer(ISender sender) : IConsumer<UserDisabled>
{
    public Task Consume(ConsumeContext<UserDisabled> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var status = context.Message.BySelf ? AccountStatus.DisabledBySelf : AccountStatus.DisabledByAdmin;
        return sender.Send(new ApplyAccountStatusCommand(context.Message.UserId, status), context.CancellationToken);
    }
}

public sealed class UserEnabledConsumer(ISender sender) : IConsumer<UserEnabled>
{
    public Task Consume(ConsumeContext<UserEnabled> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return sender.Send(new ApplyAccountStatusCommand(context.Message.UserId, AccountStatus.Active), context.CancellationToken);
    }
}

public sealed class UserErasedConsumer(ISender sender) : IConsumer<UserErased>
{
    public Task Consume(ConsumeContext<UserErased> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return sender.Send(new EraseIdentityCommand(context.Message.UserId), context.CancellationToken);
    }
}

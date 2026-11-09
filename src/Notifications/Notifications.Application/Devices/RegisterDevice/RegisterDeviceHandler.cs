using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Devices.RegisterDevice;

internal sealed class RegisterDeviceHandler(IRecipientRepository recipients, ICurrentUser currentUser, TimeProvider time)
    : ICommandHandler<RegisterDeviceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterDeviceCommand command, CancellationToken cancellationToken)
    {
        var target = PushTarget.Create(command.Endpoint, command.P256dh, command.Auth);
        if (target.IsFailed)
        {
            return target.ToResult<Guid>();
        }

        var userId = currentUser.RequiredUserId;
        foreach (var other in await recipients.FindByEndpointAsync(target.Value.Endpoint, cancellationToken))
        {
            if (other.UserId != userId)
            {
                other.ForgetEndpoint(target.Value.Endpoint);
            }
        }

        var recipient = await recipients.GetOrCreateAsync(userId, cancellationToken);
        return recipient.RegisterDevice(target.Value, command.UserAgent, time.GetUtcNow()).Id;
    }
}

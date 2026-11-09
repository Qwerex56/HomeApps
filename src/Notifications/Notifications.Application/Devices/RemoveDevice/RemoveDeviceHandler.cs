using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Devices.RemoveDevice;

internal sealed class RemoveDeviceHandler(IRecipientRepository recipients, ICurrentUser currentUser)
    : ICommandHandler<RemoveDeviceCommand>
{
    public async Task<Result> Handle(RemoveDeviceCommand command, CancellationToken cancellationToken)
    {
        var recipient = await recipients.FindAsync(currentUser.RequiredUserId, cancellationToken);
        return recipient is null ? Result.Fail(RecipientErrors.DeviceNotFound) : recipient.RemoveDevice(command.DeviceId);
    }
}

using Auth.Application.Abstractions;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Devices.IssueDeviceToken;

internal sealed class IssueDeviceTokenHandler(IDeviceIdentityRepository devices, ITokenIssuer tokens)
    : ICommandHandler<IssueDeviceTokenCommand, DeviceTokenView>
{
    public async Task<Result<DeviceTokenView>> Handle(IssueDeviceTokenCommand command, CancellationToken cancellationToken)
    {
        var identity = await devices.GetByIdAsync(command.DeviceId, cancellationToken);
        if (identity is null || !identity.Accepts(command.Secret ?? string.Empty))
        {
            return Result.Fail(DeviceIdentityErrors.InvalidCredentials);
        }

        var token = tokens.IssueDeviceToken(identity, null);
        return new DeviceTokenView(token.Value, token.ExpiresAt);
    }
}

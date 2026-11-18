using Auth.Application.Abstractions;
using Auth.Domain.Common;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Devices.IdentifySpeaker;

internal sealed class IdentifySpeakerHandler(
    IClientRegistry clients, IDeviceIdentityRepository devices, IIdentityRepository identities, IPasswordHasher hasher, TimeProvider time)
    : ICommandHandler<IdentifySpeakerCommand, SpeakerView>
{
    public async Task<Result<SpeakerView>> Handle(IdentifySpeakerCommand command, CancellationToken cancellationToken)
    {
        if (clients.AuthenticateService(command.ClientId, command.ClientSecret) is null)
        {
            return Result.Fail(DeviceIdentityErrors.ClientInvalid);
        }

        var now = time.GetUtcNow();
        var device = await devices.GetByIdAsync(command.DeviceId, cancellationToken);
        if (device is not { IsActive: true })
        {
            return Result.Fail(DeviceIdentityErrors.InvalidCredentials);
        }

        if (device.PinLocked(now))
        {
            return Result.Fail(DeviceIdentityErrors.PinLocked);
        }

        var matched = new List<Guid>();
        foreach (var id in command.Candidates.Distinct())
        {
            if (await identities.GetByIdAsync(id, cancellationToken) is { } identity && identity.AcceptsPin(command.Pin, hasher))
            {
                matched.Add(identity.Id);
            }
        }

        if (matched.Count > 1)
        {
            // Not the satellite's fault: two people chose the same PIN and no name narrowed it down.
            return Result.Fail(DeviceIdentityErrors.PinAmbiguous);
        }

        device.PinChecked(matched.Count == 1, now);
        return new SpeakerView(matched.Count == 1 ? matched[0] : null);
    }
}

using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Application.Devices;

public static class DeviceIdentityErrors
{
    public static Error ClientInvalid => new AppError(
        "devices.client_invalid", "The service client or its secret is not valid.", ErrorKind.Unauthorized);

    /// <summary>Unknown device, wrong secret or unpaired: the same answer, so nothing is learned from it.</summary>
    public static Error InvalidCredentials => new AppError(
        "devices.invalid_credentials", "This device is not paired or its secret is wrong; pair it again.", ErrorKind.Unauthorized);

    public static Error PinAmbiguous => new AppError(
        "devices.pin_ambiguous", "More than one person has this PIN; say your name with it.", ErrorKind.Conflict);

    public static Error PinLocked => new AppError(
        "devices.pin_locked", "Too many wrong PINs on this device; try again in 15 minutes.", ErrorKind.Locked);
}

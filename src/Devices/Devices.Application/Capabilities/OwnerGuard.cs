using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;

namespace Devices.Application.Capabilities;

internal static class OwnerGuard
{
    /// <summary>Registered capabilities belong to the whole instance, so only its Owner may see or change them (D14).</summary>
    public static Result RequireOwner(this ICurrentUser user) =>
        user.IsInRole("Owner") ? Result.Ok() : Result.Fail(DeviceErrors.OwnerOnly);
}

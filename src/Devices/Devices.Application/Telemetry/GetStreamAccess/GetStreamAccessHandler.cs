using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Households;

namespace Devices.Application.Telemetry.GetStreamAccess;

/// <summary>The answer is <c>true</c> when the caller sees events too (a member), <c>false</c> for a guest.</summary>
internal sealed class GetStreamAccessHandler(IHouseholdRoles households) : IQueryHandler<GetStreamAccessQuery, bool>
{
    public async Task<Result<bool>> Handle(GetStreamAccessQuery query, CancellationToken cancellationToken) =>
        await households.GetRoleAsync(query.HouseholdId, cancellationToken) is { } role ? role >= HouseholdRole.Member : Result.Fail(DeviceErrors.NotFound);
}

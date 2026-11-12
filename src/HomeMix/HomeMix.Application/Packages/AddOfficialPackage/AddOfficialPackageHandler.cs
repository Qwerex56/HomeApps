using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Packages;

namespace HomeMix.Application.Packages.AddOfficialPackage;

internal sealed class AddOfficialPackageHandler(IOfficialAddons official, PackageIntake intake)
    : ICommandHandler<AddOfficialPackageCommand, PackageView>
{
    public async Task<Result<PackageView>> Handle(AddOfficialPackageCommand command, CancellationToken cancellationToken)
    {
        var zip = await official.ReadAsync(command.AddonId, cancellationToken);
        if (zip is null)
        {
            return Result.Fail(PackageErrors.NotFound);
        }

        return (await intake.AddAsync(zip, null, AddonPackage.OfficialTag, cancellationToken)).Map(PackageView.From);
    }
}

using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;

namespace HomeMix.Application.Packages.AddPackageFromZip;

internal sealed class AddPackageFromZipHandler(PackageIntake intake) : ICommandHandler<AddPackageFromZipCommand, PackageView>
{
    public async Task<Result<PackageView>> Handle(AddPackageFromZipCommand command, CancellationToken cancellationToken) =>
        (await intake.AddAsync(command.Zip, repository: null, tag: null, cancellationToken)).Map(PackageView.From);
}

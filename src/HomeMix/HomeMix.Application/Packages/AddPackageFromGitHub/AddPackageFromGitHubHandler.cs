using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Packages.AddPackageFromGitHub;

internal sealed class AddPackageFromGitHubHandler(IGitHubReleases github, PackageIntake intake, IOptions<HomeMixOptions> options)
    : ICommandHandler<AddPackageFromGitHubCommand, PackageView>
{
    public async Task<Result<PackageView>> Handle(AddPackageFromGitHubCommand command, CancellationToken cancellationToken)
    {
        // Checked before downloading, so a switched-off instance does not even call GitHub.
        if (!options.Value.UploadsEnabled)
        {
            return Result.Fail(PackageErrors.UploadsDisabled);
        }

        var release = await github.DownloadAsync(command.Repository, command.Tag, cancellationToken);
        if (release.IsFailed)
        {
            return release.ToResult();
        }

        return (await intake.AddAsync(release.Value.Zip, release.Value.Repository, release.Value.Tag, cancellationToken)).Map(PackageView.From);
    }
}

using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Installations.Install;

internal sealed class InstallHandler(
    IPackageRepository packages,
    IInstallationRepository installations,
    InstallationAccess access,
    ICurrentUser currentUser,
    TimeProvider time,
    IOptions<HomeMixOptions> options) : ICommandHandler<InstallCommand, InstallationView>
{
    public async Task<Result<InstallationView>> Handle(InstallCommand command, CancellationToken cancellationToken)
    {
        var target = command.Target == TargetKind.Household
            ? InstallTarget.Household(command.HouseholdId!.Value)
            : InstallTarget.User(currentUser.RequiredUserId);
        if (!await access.CanManageAsync(target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.Forbidden);
        }

        if (!options.Value.UploadsEnabled)
        {
            return Result.Fail(PackageErrors.UploadsDisabled);
        }

        var package = await packages.FindAsync(command.Sha256.ToLowerInvariant(), cancellationToken);
        if (package is null)
        {
            return Result.Fail(PackageErrors.NotFound);
        }

        if (options.Value.IsBlocked(package.AddonId, package.Sha256))
        {
            return Result.Fail(PackageErrors.Blocked);
        }

        if (await installations.FindActiveAsync(package.AddonId, target, cancellationToken) is not null)
        {
            return Result.Fail(InstallationErrors.AlreadyInstalled);
        }

        var installation = Installation.Install(package, target, command.Consent.ToConsent(), currentUser.RequiredUserId, time.GetUtcNow());
        if (installation.IsFailed)
        {
            return installation.ToResult();
        }

        installations.Add(installation.Value);
        return InstallationView.From(installation.Value, package);
    }
}

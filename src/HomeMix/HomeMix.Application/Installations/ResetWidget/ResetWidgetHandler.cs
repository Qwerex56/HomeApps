using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;

namespace HomeMix.Application.Installations.ResetWidget;

internal sealed class ResetWidgetHandler(IInstallationRepository installations, IPackageRepository packages, InstallationAccess access)
    : ICommandHandler<ResetWidgetCommand, InstallationView>
{
    public async Task<Result<InstallationView>> Handle(ResetWidgetCommand command, CancellationToken cancellationToken)
    {
        var found = await WidgetSettings.FindAsync(command.InstallationId, installations, packages, access, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        var (installation, package) = found.Value;
        var reset = installation.ResetWidget(command.Place);
        return reset.IsFailed ? reset : InstallationView.From(installation, package);
    }
}

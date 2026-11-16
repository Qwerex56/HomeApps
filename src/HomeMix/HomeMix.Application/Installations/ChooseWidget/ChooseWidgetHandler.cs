using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Installations.ChooseWidget;

internal sealed class ChooseWidgetHandler(IInstallationRepository installations, IPackageRepository packages, InstallationAccess access)
    : ICommandHandler<ChooseWidgetCommand, InstallationView>
{
    public async Task<Result<InstallationView>> Handle(ChooseWidgetCommand command, CancellationToken cancellationToken)
    {
        var found = await WidgetSettings.FindAsync(command.InstallationId, installations, packages, access, cancellationToken);
        if (found.IsFailed)
        {
            return found.ToResult();
        }

        var (installation, package) = found.Value;
        var chosen = installation.ChooseWidget(package.Manifest, command.Place, command.WidgetId, command.Size, command.Order);
        return chosen.IsFailed ? chosen : InstallationView.From(installation, package);
    }
}

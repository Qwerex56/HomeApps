using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Realtime;

namespace HomeMix.Application.Installations.ResetWidget;

internal sealed class ResetWidgetHandler(IInstallationRepository installations, IPackageRepository packages, InstallationAccess access, RealtimeOutbox realtime)
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
        realtime.Add(RealtimeEvents.InstallationChanged(installation, "widgets"));
        return reset.IsFailed ? reset : InstallationView.From(installation, package);
    }
}

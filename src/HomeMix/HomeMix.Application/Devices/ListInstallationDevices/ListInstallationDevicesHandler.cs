using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Domain.Devices;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Devices.ListInstallationDevices;

internal sealed class ListInstallationDevicesHandler(DevicesAccess access, IHouseholdDevices devices)
    : IQueryHandler<ListInstallationDevicesQuery, InstallationDevicesView>
{
    public async Task<Result<InstallationDevicesView>> Handle(ListInstallationDevicesQuery query, CancellationToken cancellationToken)
    {
        var opened = await access.OpenAsync(query.InstallationId, AddonCapabilities.Devices, cancellationToken);
        if (opened.IsFailed)
        {
            return opened.ToResult();
        }

        var installation = opened.Value;
        if (query.WidgetId is null)
        {
            var all = await devices.ListAsync(installation.TargetId, query.Capability, query.Role, query.Model, query.WithState, cancellationToken);
            return new InstallationDevicesView(all, null, false);
        }

        var package = await access.PackageOfAsync(installation, cancellationToken);
        if (package.IsFailed)
        {
            return package.ToResult();
        }

        var widget = package.Value.Manifest.Widgets.FirstOrDefault(w => w.Id == query.WidgetId && w.Devices.Count > 0);
        if (widget is null)
        {
            return Result.Fail(InstallationErrors.WidgetWithoutDevices(query.WidgetId));
        }

        var household = await devices.ListAsync(installation.TargetId, null, null, null, query.WithState, cancellationToken);
        var picked = installation.DevicePicks.FirstOrDefault(p => p.WidgetId == widget.Id)?.DeviceId;
        var matches = DeviceMatcher.Match(widget.Devices, household.Select(d => d.ToCandidate()), picked);
        var byId = household.ToDictionary(d => d.Id);
        return new InstallationDevicesView([.. matches.Candidates.Select(c => byId[c.Id])], matches.Selected, matches.NeedsPick);
    }
}

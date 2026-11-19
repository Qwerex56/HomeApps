using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Installations;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Triggers;

namespace HomeMix.Application.Triggers.SetTriggerEnabled;

internal sealed class SetTriggerEnabledHandler(
    IInstallationRepository installations, IPackageRepository packages, ITriggerStates states, InstallationAccess access)
    : ICommandHandler<SetTriggerEnabledCommand, TriggerView>
{
    public async Task<Result<TriggerView>> Handle(SetTriggerEnabledCommand command, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(command.InstallationId, cancellationToken);
        if (installation is null || !installation.IsActive || installation.TargetKind != TargetKind.Household
            || !await access.CanUseAsync(installation.Target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!await access.CanManageAsync(installation.Target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.Forbidden);
        }

        var manifest = (await packages.FindAsync(installation.PackageSha256, cancellationToken))?.Manifest;
        var trigger = manifest?.Triggers.FirstOrDefault(t => t.Id == command.TriggerId);
        if (manifest is null || trigger is null)
        {
            return Result.Fail(TriggerErrors.NotFound);
        }

        var state = await states.FindAsync(installation.Id, trigger.Id, cancellationToken);
        if (state is null)
        {
            state = TriggerState.For(installation.Id, trigger.Id);
            states.Add(state);
        }

        if (command.Enabled)
        {
            state.Enable();
        }
        else
        {
            state.Disable();
        }

        return new TriggerView(
            installation.Id, installation.AddonId, manifest.Name, trigger.Id, trigger.On.ToString().ToLowerInvariant(),
            trigger.Match.Capability, trigger.Match.Role, trigger.Match.Model, trigger.Field, trigger.Name,
            state.Enabled, state.ConsecutiveFailures, state.LastRunAt, state.LastError, state.DisabledByFailures);
    }
}

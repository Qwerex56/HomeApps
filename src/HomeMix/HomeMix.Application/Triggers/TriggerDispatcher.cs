using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Notifications;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Triggers;
using Microsoft.Extensions.Logging;

namespace HomeMix.Application.Triggers;

/// <summary>
/// Runs the triggers of a household's add-ons for a device's change or event (household-jarvis-devices.md 2.6). Only active,
/// unblocked household installations that were granted <c>devices</c>; a trigger fires when the device and the field or
/// event match, it is enabled, the change was not this installation's own set (loop guard) and the installation is under its
/// run limit. A failing script counts; <see cref="TriggerState.MaxFailures"/> in a row turn the trigger off and tell the admins.
/// </summary>
public sealed partial class TriggerDispatcher(
    IInstallationRepository installations,
    IPackageRepository packages,
    Blocklist blocklist,
    ITriggerStates states,
    ITriggerScripts scripts,
    IHouseholdAdmins admins,
    TriggerRunLimiter limiter,
    IEventBus bus,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    ILogger<TriggerDispatcher> logger)
{
    public async Task DispatchAsync(TriggerOccurrence occurrence, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        var candidates = (await installations.ListActiveAsync(InstallTarget.Household(occurrence.HouseholdId), cancellationToken))
            .Where(i => i.Capabilities.Contains(AddonCapabilities.Devices))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        var bySha = (await packages.FindManyAsync([.. candidates.Select(i => i.PackageSha256).Distinct()], cancellationToken))
            .ToDictionary(p => p.Sha256, StringComparer.Ordinal);
        var isBlocked = await blocklist.SnapshotAsync(cancellationToken);
        var ran = false;
        foreach (var installation in candidates)
        {
            if (!bySha.TryGetValue(installation.PackageSha256, out var package) || isBlocked(installation.AddonId, installation.PackageSha256))
            {
                continue;
            }

            foreach (var trigger in package.Manifest.Triggers.Where(t => Matches(t, occurrence)))
            {
                ran |= await RunAsync(new TriggerRun(installation, package.Manifest, trigger, occurrence), cancellationToken);
            }
        }

        if (ran)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool Matches(AddonTrigger trigger, TriggerOccurrence occurrence) => occurrence.Kind == TriggerKind.State
        ? TriggerMatcher.MatchesState(trigger, occurrence.Device, occurrence.ChangedValues)
        : TriggerMatcher.MatchesEvent(trigger, occurrence.Device, occurrence.EventName ?? string.Empty);

    /// <summary>True when the script ran (and its outcome is to be saved).</summary>
    private async Task<bool> RunAsync(TriggerRun run, CancellationToken cancellationToken)
    {
        var installation = run.Installation;
        var state = await states.FindAsync(installation.Id, run.Trigger.Id, cancellationToken);
        if (state is { Enabled: false })
        {
            return false;
        }

        if (run.Occurrence.ChangedBy == TriggerSource.For(installation.Id))
        {
            LogOwnChange(logger, installation.AddonId, run.Trigger.Id);
            return false;
        }

        if (!limiter.TryAcquire(installation.Id))
        {
            LogRateLimited(logger, installation.AddonId, TriggerRunLimiter.PerMinute);
            return false;
        }

        if (state is null)
        {
            state = TriggerState.For(installation.Id, run.Trigger.Id);
            states.Add(state);
        }

        var error = await TryRunAsync(run, cancellationToken);
        var now = time.GetUtcNow();
        if (error is null)
        {
            state.Succeeded(now);
            return true;
        }

        LogFailed(logger, installation.AddonId, run.Trigger.Id, error);
        if (state.Failed(error, now))
        {
            await TellAdminsAsync(run, error, cancellationToken);
        }

        return true;
    }

    private async Task<string?> TryRunAsync(TriggerRun run, CancellationToken cancellationToken)
    {
        try
        {
            var result = await scripts.RunAsync(run, cancellationToken);
            return result.IsSuccess ? null : result.Errors[0].Message;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return exception.Message;
        }
    }

    private async Task TellAdminsAsync(TriggerRun run, string error, CancellationToken cancellationToken)
    {
        var installation = run.Installation;
        IReadOnlyList<Guid> people;
        try
        {
            people = await admins.GetAdminIdsAsync(installation.TargetId, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogAdminsUnavailable(logger, installation.TargetId, exception);
            return;
        }

        if (people.Count > 0)
        {
            await bus.PublishAsync(
                new NotificationRequested(
                    people, installation.AddonId, "trigger", run.Manifest.Name.Pl,
                    $"Wyzwalacz „{run.Trigger.Id}” wyłączył się po {TriggerState.MaxFailures} błędach z rzędu: {error}",
                    $"/homes/{installation.TargetId}/devices", installation.TargetId, Tag: $"trigger-{installation.Id}-{run.Trigger.Id}"),
                cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "The trigger {AddonId}/{TriggerId} skipped a change its own add-on made.")]
    private static partial void LogOwnChange(ILogger logger, string addonId, string triggerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The add-on {AddonId} reached its limit of {PerMinute} trigger runs a minute; a run is skipped.")]
    private static partial void LogRateLimited(ILogger logger, string addonId, int perMinute);

    [LoggerMessage(Level = LogLevel.Information, Message = "The trigger {AddonId}/{TriggerId} failed: {Error}")]
    private static partial void LogFailed(ILogger logger, string addonId, string triggerId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The admins of household {HouseholdId} could not be asked; the notice about a disabled trigger is dropped.")]
    private static partial void LogAdminsUnavailable(ILogger logger, Guid householdId, Exception exception);
}

using FluentResults;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Installations;
using HomeMix.Domain.Installations;

namespace HomeMix.Application.Triggers.ListHouseholdTriggers;

internal sealed class ListHouseholdTriggersHandler(
    IInstallationRepository installations, IPackageRepository packages, ITriggerStates states, InstallationAccess access)
    : IQueryHandler<ListHouseholdTriggersQuery, IReadOnlyList<TriggerView>>
{
    public async Task<Result<IReadOnlyList<TriggerView>>> Handle(ListHouseholdTriggersQuery query, CancellationToken cancellationToken)
    {
        var target = InstallTarget.Household(query.HouseholdId);
        if (!await access.CanUseAsync(target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!await access.CanManageAsync(target, cancellationToken))
        {
            return Result.Fail(InstallationErrors.Forbidden);
        }

        var active = await installations.ListActiveAsync(target, cancellationToken);
        var bySha = (await packages.FindManyAsync([.. active.Select(i => i.PackageSha256).Distinct()], cancellationToken))
            .ToDictionary(p => p.Sha256, StringComparer.Ordinal);
        var saved = (await states.ListAsync([.. active.Select(i => i.Id)], cancellationToken)).ToDictionary(s => (s.InstallationId, s.TriggerId));
        var views = new List<TriggerView>();
        foreach (var installation in active.Where(i => bySha.ContainsKey(i.PackageSha256)))
        {
            var manifest = bySha[installation.PackageSha256].Manifest;
            foreach (var trigger in manifest.Triggers)
            {
                saved.TryGetValue((installation.Id, trigger.Id), out var state);
                views.Add(new TriggerView(
                    installation.Id, installation.AddonId, manifest.Name, trigger.Id, trigger.On.ToString().ToLowerInvariant(),
                    trigger.Match.Capability, trigger.Match.Role, trigger.Match.Model, trigger.Field, trigger.Name,
                    state?.Enabled ?? true, state?.ConsecutiveFailures ?? 0, state?.LastRunAt, state?.LastError, state?.DisabledByFailures ?? false));
            }
        }

        return Result.Ok<IReadOnlyList<TriggerView>>([.. views.OrderBy(v => v.AddonName.Pl, StringComparer.CurrentCulture).ThenBy(v => v.Id, StringComparer.Ordinal)]);
    }
}

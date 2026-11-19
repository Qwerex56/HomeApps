using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Triggers;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using HomeMix.Domain.Triggers;

namespace HomeMix.Application.Tests;

internal sealed class TriggerInstallations(params Installation[] items) : IInstallationRepository
{
    public Task<Installation?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(items.FirstOrDefault(i => i.Id == id));

    public Task<Installation?> FindActiveAsync(string addonId, InstallTarget target, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Installation>> ListActiveAsync(InstallTarget target, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Installation>>([.. items.Where(i => i.IsActive && i.Target == target)]);

    public Task<IReadOnlyList<Installation>> ListActiveForAsync(IReadOnlyCollection<Guid> householdIds, Guid userId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Installation>> ListAllActiveAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Guid>> RemovedSinceAsync(string addonId, InstallTarget target, DateTimeOffset since, CancellationToken cancellationToken) => throw new NotSupportedException();

    public void Add(Installation installation) => throw new NotSupportedException();
}

internal sealed class TriggerPackages(params AddonPackage[] items) : IPackageRepository
{
    public Task<AddonPackage?> FindAsync(string sha256, CancellationToken cancellationToken) => Task.FromResult(items.FirstOrDefault(p => p.Sha256 == sha256));

    public Task<IReadOnlyList<AddonPackage>> FindManyAsync(IReadOnlyCollection<string> sha256s, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AddonPackage>>([.. items.Where(p => sha256s.Contains(p.Sha256))]);

    public void Add(AddonPackage package) => throw new NotSupportedException();
}

internal sealed class FakeTriggerStates : ITriggerStates
{
    public List<TriggerState> All { get; } = [];

    public Task<TriggerState?> FindAsync(Guid installationId, string triggerId, CancellationToken cancellationToken) =>
        Task.FromResult(All.Find(s => s.InstallationId == installationId && s.TriggerId == triggerId));

    public Task<IReadOnlyList<TriggerState>> ListAsync(IReadOnlyCollection<Guid> installationIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TriggerState>>([.. All.Where(s => installationIds.Contains(s.InstallationId))]);

    public void Add(TriggerState state) => All.Add(state);

    public Task RemoveForInstallationAsync(Guid installationId, CancellationToken cancellationToken)
    {
        All.RemoveAll(s => s.InstallationId == installationId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeTriggerScripts : ITriggerScripts
{
    public List<TriggerRun> Runs { get; } = [];

    public string? Fails { get; set; }

    public Task<Result> RunAsync(TriggerRun run, CancellationToken cancellationToken)
    {
        Runs.Add(run);
        return Task.FromResult(Fails is null ? Result.Ok() : Result.Fail(Fails));
    }
}

internal sealed class FakeAdmins(params Guid[] admins) : IHouseholdAdmins
{
    public Task<IReadOnlyList<Guid>> GetAdminIdsAsync(Guid householdId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Guid>>(admins);
}

internal sealed class TriggerBus : IEventBus
{
    public List<object> Published { get; } = [];

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class, IIntegrationEvent
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class CountingUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.FromResult(0);
    }
}

internal sealed class ClockAt(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

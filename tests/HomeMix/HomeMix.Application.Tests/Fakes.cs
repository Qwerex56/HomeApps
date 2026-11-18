using System.Text.Json;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;
using HomeMix.Application;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Blocks;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Packages;
using Microsoft.Extensions.Options;

namespace HomeMix.Application.Tests;

internal sealed class FakeInstallations(params Installation[] items) : IInstallationRepository
{
    public Task<Installation?> FindAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(items.FirstOrDefault(i => i.Id == id));

    public Task<Installation?> FindActiveAsync(string addonId, InstallTarget target, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Installation>> ListActiveAsync(InstallTarget target, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Installation>> ListActiveForAsync(IReadOnlyCollection<Guid> householdIds, Guid userId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Installation>> ListAllActiveAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Guid>> RemovedSinceAsync(string addonId, InstallTarget target, DateTimeOffset since, CancellationToken cancellationToken) => throw new NotSupportedException();

    public void Add(Installation installation) => throw new NotSupportedException();
}

internal sealed class FakePackages(params AddonPackage[] items) : IPackageRepository
{
    public Task<AddonPackage?> FindAsync(string sha256, CancellationToken cancellationToken) => Task.FromResult(items.FirstOrDefault(p => p.Sha256 == sha256));

    public Task<IReadOnlyList<AddonPackage>> FindManyAsync(IReadOnlyCollection<string> sha256s, CancellationToken cancellationToken) => throw new NotSupportedException();

    public void Add(AddonPackage package) => throw new NotSupportedException();
}

internal sealed class FakeRoles(HouseholdRole? role) : IHouseholdRoles
{
    public Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken) => Task.FromResult(role);

    public Task<IReadOnlyList<HouseholdMember>> GetMembersAsync(Guid householdId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<MyHousehold>> GetMyHouseholdsAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class FakeBlocks : IBlockRepository
{
    public Task<IReadOnlyList<AddonBlock>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AddonBlock>>([]);

    public Task<AddonBlock?> FindAsync(string value, CancellationToken cancellationToken) => Task.FromResult<AddonBlock?>(null);

    public Task<bool> AnyAsync(string addonId, string sha256, CancellationToken cancellationToken) => Task.FromResult(false);

    public void Add(AddonBlock block) => throw new NotSupportedException();

    public void Remove(AddonBlock block) => throw new NotSupportedException();

    public static Blocklist Blocklist() => new(new FakeBlocks(), Options.Create(new HomeMixOptions()));
}

internal sealed class FakeUser(Guid id) : ICurrentUser
{
    public bool IsAuthenticated => true;

    public Guid? UserId => id;

    public string? Email => null;

    public DateTimeOffset? AuthenticatedAt => null;

    public IReadOnlyCollection<string> Roles => [];

    public bool IsInRole(string role) => false;

    public Guid RequiredUserId => id;
}

/// <summary>Devices as a person sees it: the household's devices, their states, and a log of what was set.</summary>
internal sealed class FakeDevices(params HouseholdDevice[] devices) : IHouseholdDevices
{
    public List<(Guid Device, string Control, JsonElement Value)> Sets { get; } = [];

    public Task<IReadOnlyList<HouseholdDevice>> ListAsync(
        Guid householdId, string? capability, string? role, string? model, bool withState, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HouseholdDevice>>(devices);

    public Task<JsonElement?> GetStateAsync(Guid deviceId, CancellationToken cancellationToken) =>
        Task.FromResult<JsonElement?>(JsonDocument.Parse("""{"values":{"on":true}}""").RootElement);

    public Task<Result> SetAsync(Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken)
    {
        Sets.Add((deviceId, control, value));
        return Task.FromResult(Result.Ok());
    }
}

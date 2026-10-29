using HomeApps.Contracts.Households;
using HomeApps.Plugins.Hosting;
using Microsoft.Extensions.Options;

namespace HomeApps.Plugins.Tests;

public sealed class AccessCacheTests
{
    private readonly ManualTime _time = new();
    private readonly HouseholdAccessCache _cache;
    private readonly Guid _household = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    public AccessCacheTests() => _cache = new HouseholdAccessCache(_time, Options.Create(new PluginHostingOptions()));

    [Fact]
    public void A_role_is_kept_for_a_minute()
    {
        _cache.Set(_household, _user, HouseholdRole.Member);

        _time.Advance(TimeSpan.FromSeconds(59));
        Assert.True(_cache.TryGet(_household, _user, out var role));
        Assert.Equal(HouseholdRole.Member, role);

        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.False(_cache.TryGet(_household, _user, out _));
    }

    [Fact]
    public void No_access_is_kept_only_briefly()
    {
        _cache.Set(_household, _user, null);

        Assert.True(_cache.TryGet(_household, _user, out var role));
        Assert.Null(role);

        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.False(_cache.TryGet(_household, _user, out _));
    }

    [Fact]
    public void Forgetting_a_household_drops_all_its_members_only()
    {
        var other = Guid.NewGuid();
        _cache.Set(_household, _user, HouseholdRole.Member);
        _cache.Set(_household, Guid.NewGuid(), HouseholdRole.Admin);
        _cache.Set(other, _user, HouseholdRole.Owner);

        _cache.Forget(_household);

        Assert.False(_cache.TryGet(_household, _user, out _));
        Assert.True(_cache.TryGet(other, _user, out _));
    }

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}

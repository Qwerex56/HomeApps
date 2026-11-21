using MacDeviceEmulator.Protocol;
using MacDeviceEmulator.Services;
using Xunit;

namespace MacDeviceEmulator.Tests;

public sealed class PendingConfirmationStoreTests
{
    private readonly PendingConfirmationStore _store = new();
    private readonly DateTimeOffset _now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddFromReply_stores_unexpired_actions_requiring_confirmation()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var actions = new List<VoiceActionView>
        {
            new(id1, "computer.set_volume", "Ustaw głośność na 80%", "NeedsConfirmation", _now.AddMinutes(5)),
            new(id2, "computer.say", "Powiedz coś", "Approved", _now.AddMinutes(5)),
            new(id3, "homemix.lights", "Wyłącz światła", "NeedsConfirmation", _now.AddMinutes(-1)) // already expired
        };

        var added = _store.AddFromReply(actions, _now);

        Assert.Single(added);
        Assert.Equal(id1, added[0].Id);
        Assert.True(_store.HasPending(_now));

        var active = _store.GetActive(_now);
        Assert.Single(active);
        Assert.Equal(id1, active[0].Id);
    }

    [Fact]
    public void HasPending_returns_false_when_actions_expire()
    {
        var id = Guid.NewGuid();
        _store.AddFromReply([new(id, "tool", "desc", "NeedsConfirmation", _now.AddSeconds(30))], _now);

        Assert.True(_store.HasPending(_now));
        Assert.False(_store.HasPending(_now.AddSeconds(31)));
    }

    [Fact]
    public void Remove_removes_action()
    {
        var id = Guid.NewGuid();
        _store.AddFromReply([new(id, "tool", "desc", "NeedsConfirmation", _now.AddMinutes(5))], _now);

        Assert.True(_store.HasPending(_now));
        _store.Remove(id);
        Assert.False(_store.HasPending(_now));
    }

    [Fact]
    public void ResolveTarget_resolves_by_guid_or_index_or_default()
    {
        var id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        _store.AddFromReply(
        [
            new(id1, "tool1", "desc1", "NeedsConfirmation", _now.AddMinutes(5)),
            new(id2, "tool2", "desc2", "NeedsConfirmation", _now.AddMinutes(5))
        ], _now);

        // Exact GUID
        var target1 = _store.ResolveTarget(id1.ToString(), _now);
        Assert.NotNull(target1);
        Assert.Equal(id1, target1.Id);

        // 1-based index
        var targetIndex1 = _store.ResolveTarget("1", _now);
        Assert.NotNull(targetIndex1);
        Assert.Equal(id1, targetIndex1.Id);

        var targetIndex2 = _store.ResolveTarget("2", _now);
        Assert.NotNull(targetIndex2);
        Assert.Equal(id2, targetIndex2.Id);

        // GUID prefix
        var targetPrefix = _store.ResolveTarget("2222", _now);
        Assert.NotNull(targetPrefix);
        Assert.Equal(id2, targetPrefix.Id);

        // Null token with multiple pending returns null (must specify target)
        var targetDefaultMulti = _store.ResolveTarget(null, _now);
        Assert.Null(targetDefaultMulti);

        // Remove one so only 1 remains: null token resolves that single item
        _store.Remove(id1);
        var targetDefaultSingle = _store.ResolveTarget(null, _now);
        Assert.NotNull(targetDefaultSingle);
        Assert.Equal(id2, targetDefaultSingle.Id);
    }
}

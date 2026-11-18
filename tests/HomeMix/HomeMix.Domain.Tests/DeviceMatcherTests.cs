using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;

namespace HomeMix.Domain.Tests;

public sealed class DeviceMatcherTests
{
    private static DeviceCandidate Device(string name, bool online = true, string? model = null, string[]? roles = null, string[]? capabilities = null) =>
        new(Guid.NewGuid(), name, online, model, roles ?? [], capabilities ?? []);

    private static DeviceMatch ByCapability(string capability, string? pick = null) => new(capability, null, null, pick);

    private static DeviceMatch ByRole(string role, string? pick = null) => new(null, role, null, pick);

    private static DeviceMatch ByModel(string model, string? pick = null) => new(null, null, model, pick);

    [Fact]
    public void Capabilities_beat_roles_and_roles_beat_model()
    {
        var byModel = Device("a", model: "x/stacja");
        var byRole = Device("b", roles: ["weather_station"]);
        var byCapability = Device("c", capabilities: ["weather"]);

        var result = DeviceMatcher.Match(
            [ByModel("x/stacja"), ByRole("weather_station"), ByCapability("weather")], [byModel, byRole, byCapability], picked: null);

        Assert.Equal([byCapability, byRole, byModel], result.Candidates);
        Assert.Equal(byCapability.Id, result.Selected);
        Assert.False(result.NeedsPick);
    }

    [Fact]
    public void Among_equals_the_first_online_wins_then_the_name()
    {
        var offline = Device("a", online: false, capabilities: ["weather"]);
        var later = Device("z", capabilities: ["weather"]);
        var earlier = Device("m", capabilities: ["weather"]);

        var result = DeviceMatcher.Match([ByCapability("weather")], [offline, later, earlier], null);

        Assert.Equal([earlier, later, offline], result.Candidates);
        Assert.Equal(earlier.Id, result.Selected);
    }

    [Fact]
    public void A_capability_matches_with_or_without_an_instance_id_and_only_whole_names()
    {
        var lamp = Device("lampa", capabilities: ["switch:lampka"]);
        var other = Device("inne", capabilities: ["switchboard"]);

        var result = DeviceMatcher.Match([ByCapability("switch")], [lamp, other], null);

        Assert.Equal([lamp], result.Candidates);
    }

    [Fact]
    public void Every_field_of_an_entry_must_match()
    {
        var both = Device("a", roles: ["weather_station"], capabilities: ["weather"]);
        var onlyCapability = Device("b", capabilities: ["weather"]);

        var result = DeviceMatcher.Match([new DeviceMatch("weather", "weather_station", null, null)], [both, onlyCapability], null);

        Assert.Equal([both], result.Candidates);
    }

    [Fact]
    public void A_weather_widget_never_lists_a_relay()
    {
        var relay = Device("przekaznik", capabilities: ["switch"]);

        var result = DeviceMatcher.Match([ByCapability("weather", pick: "manual")], [relay], null);

        Assert.Empty(result.Candidates);
        Assert.Null(result.Selected);
        Assert.False(result.NeedsPick);
    }

    [Fact]
    public void A_manual_entry_is_a_list_to_choose_from_until_someone_picks()
    {
        var one = Device("a", capabilities: ["weather"]);
        var two = Device("b", capabilities: ["weather"]);

        var undecided = DeviceMatcher.Match([ByCapability("weather", pick: "manual")], [one, two], null);
        var decided = DeviceMatcher.Match([ByCapability("weather", pick: "manual")], [one, two], two.Id);

        Assert.Equal(2, undecided.Candidates.Count);
        Assert.Null(undecided.Selected);
        Assert.True(undecided.NeedsPick);
        Assert.Equal(two.Id, decided.Selected);
        Assert.False(decided.NeedsPick);
    }

    [Fact]
    public void Automatic_matches_come_before_a_manual_fallback()
    {
        var auto = Device("z", roles: ["weather_station"]);
        var manual = Device("a", capabilities: ["weather"]);

        var result = DeviceMatcher.Match([ByRole("weather_station"), ByCapability("weather", pick: "manual")], [auto, manual], null);

        Assert.Equal([auto, manual], result.Candidates);
        Assert.Equal(auto.Id, result.Selected);
    }

    [Fact]
    public void A_pick_that_is_no_longer_a_candidate_is_ignored()
    {
        var device = Device("a", capabilities: ["weather"]);

        var result = DeviceMatcher.Match([ByCapability("weather")], [device], Guid.NewGuid());

        Assert.Equal(device.Id, result.Selected);
    }

    [Fact]
    public void A_pick_beats_the_best_match_even_for_an_automatic_widget()
    {
        var best = Device("a", capabilities: ["weather"]);
        var chosen = Device("b", capabilities: ["weather"]);

        var result = DeviceMatcher.Match([ByCapability("weather")], [best, chosen], chosen.Id);

        Assert.Equal(chosen.Id, result.Selected);
    }
}

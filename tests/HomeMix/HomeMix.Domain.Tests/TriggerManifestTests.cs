using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Triggers;

namespace HomeMix.Domain.Tests;

public sealed class TriggerManifestTests
{
    private static FluentResults.Result<AddonManifest> Parse(string triggers, string capabilities = """["devices", "devices.control"]""") =>
        AddonManifestParser.Parse(Manifests.Valid(capabilities: capabilities, extra: $$""" "triggers": {{triggers}} """));

    private static string Problems(FluentResults.Result<AddonManifest> result) => string.Join(" | ", result.Errors.Select(e => e.Message));

    private static DeviceCandidate Device(string[]? capabilities = null, string[]? roles = null, string? model = null) =>
        new(Guid.NewGuid(), "Stacja", true, model, roles ?? [], capabilities ?? []);

    [Fact]
    public void Triggers_are_read_with_the_same_matching_as_widgets()
    {
        var manifest = Parse("""
            [ { "on": "state", "capability": "weather", "field": "temperature", "script": "triggers/cold.js" },
              { "on": "event", "role": "doorbell", "name": "pressed", "script": "triggers/bell.js", "id": "dzwonek" },
              { "on": "event", "model": "qwerex56/pilot-v1", "script": "triggers/remote.js" } ]
            """).Value;

        Assert.Equal(3, manifest.Triggers.Count);
        var cold = manifest.Triggers[0];
        Assert.Equal(("cold", TriggerKind.State, "weather", "temperature", "triggers/cold.js"), (cold.Id, cold.On, cold.Match.Capability, cold.Field, cold.Script));
        Assert.Equal(("dzwonek", TriggerKind.Event, "doorbell", "pressed"), (manifest.Triggers[1].Id, manifest.Triggers[1].On, manifest.Triggers[1].Match.Role, manifest.Triggers[1].Name));
        Assert.Equal("qwerex56/pilot-v1", manifest.Triggers[2].Match.Model);
    }

    [Fact]
    public void An_add_on_without_triggers_has_an_empty_list()
    {
        Assert.Empty(AddonManifestParser.Parse(Manifests.Valid()).Value.Triggers);
    }

    [Fact]
    public void Triggers_need_the_devices_capability()
    {
        var result = Parse("""[ { "on": "state", "capability": "weather", "script": "triggers/cold.js" } ]""", capabilities: """["storage.household"]""");

        Assert.Contains("needs the capability devices", Problems(result), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""[ { "on": "tick", "capability": "weather", "script": "triggers/a.js" } ]""", "on must be")]
    [InlineData("""[ { "on": "state", "script": "triggers/a.js" } ]""", "needs a capability, a role or a model")]
    [InlineData("""[ { "on": "state", "capability": "Weather", "script": "triggers/a.js" } ]""", "capability must be")]
    [InlineData("""[ { "on": "state", "capability": "weather" } ]""", "script must be")]
    [InlineData("""[ { "on": "state", "capability": "weather", "script": "tools/a.js" } ]""", "script must be")]
    [InlineData("""[ { "on": "state", "capability": "weather", "script": "triggers/../a.js" } ]""", "script must be")]
    [InlineData("""[ { "on": "state", "capability": "weather", "script": "triggers/a.html" } ]""", "script must be")]
    [InlineData("""[ { "on": "state", "capability": "weather", "name": "x", "script": "triggers/a.js" } ]""", "name is for")]
    [InlineData("""[ { "on": "event", "capability": "button", "field": "x", "script": "triggers/a.js" } ]""", "field is for")]
    [InlineData("""[ { "on": "state", "capability": "weather", "script": "triggers/a.js", "id": "A b" } ]""", "id must be")]
    [InlineData("""[ { "on": "state", "capability": "weather", "script": "triggers/a.js" }, { "on": "state", "role": "x", "script": "triggers/a.js" } ]""", "used twice")]
    [InlineData("""{ "on": "state" }""", "triggers must be an array")]
    [InlineData("""[ 3 ]""", "must be an object")]
    public void A_broken_trigger_is_reported(string triggers, string problem)
    {
        var result = Parse(triggers);

        Assert.True(result.IsFailed);
        Assert.Contains(problem, Problems(result), StringComparison.Ordinal);
    }

    [Fact]
    public void At_most_ten_triggers()
    {
        var eleven = string.Join(',', Enumerable.Range(0, 11).Select(i => $$"""{ "on": "state", "capability": "weather", "script": "triggers/t{{i}}.js" }"""));

        Assert.Contains("at most 10", Problems(Parse($"[{eleven}]")), StringComparison.Ordinal);
    }

    [Fact]
    public void A_trigger_script_must_be_in_the_package()
    {
        var manifest = Parse("""[ { "on": "state", "capability": "weather", "script": "triggers/cold.js" } ]""").Value;

        var problems = HomeMix.Domain.Packages.PackageRules.Check([new("index.html", 10)], manifest);

        Assert.Contains(problems, p => p.Contains("triggers/cold.js", StringComparison.Ordinal));
    }

    [Fact]
    public void A_state_trigger_needs_its_field_among_the_changed_values()
    {
        var trigger = Parse("""[ { "on": "state", "capability": "weather", "field": "temperature", "script": "triggers/cold.js" } ]""").Value.Triggers[0];
        var weather = Device(["weather", "power"]);

        Assert.True(TriggerMatcher.MatchesState(trigger, weather, ["temperature", "humidity"]));
        Assert.False(TriggerMatcher.MatchesState(trigger, weather, ["humidity"]));
        Assert.False(TriggerMatcher.MatchesState(trigger, Device(["switch"]), ["temperature"]));
        Assert.False(TriggerMatcher.MatchesEvent(trigger, weather, "temperature"));
    }

    [Fact]
    public void A_state_trigger_without_a_field_fires_on_any_change_and_instances_match_by_their_value_name()
    {
        var any = Parse("""[ { "on": "state", "capability": "switch", "script": "triggers/a.js" } ]""").Value.Triggers[0];
        var on = Parse("""[ { "on": "state", "capability": "switch", "field": "on", "script": "triggers/a.js" } ]""").Value.Triggers[0];
        var lamp = Device(["switch:lampka"]);

        Assert.True(TriggerMatcher.MatchesState(any, lamp, ["whatever"]));
        Assert.True(TriggerMatcher.MatchesState(on, lamp, ["lampka.on"]));
        Assert.False(TriggerMatcher.MatchesState(on, lamp, ["lampka.level"]));
    }

    [Fact]
    public void An_event_trigger_matches_the_event_name_when_it_has_one_and_the_role_and_model_too()
    {
        var named = Parse("""[ { "on": "event", "role": "doorbell", "model": "a/b", "name": "pressed", "script": "triggers/a.js" } ]""").Value.Triggers[0];
        var bell = Device(roles: ["doorbell"], model: "a/b");

        Assert.True(TriggerMatcher.MatchesEvent(named, bell, "pressed"));
        Assert.False(TriggerMatcher.MatchesEvent(named, bell, "motion"));
        Assert.False(TriggerMatcher.MatchesEvent(named, Device(roles: ["doorbell"], model: "other"), "pressed"));
        Assert.False(TriggerMatcher.MatchesState(named, bell, ["pressed"]));
    }

    [Fact]
    public void A_trigger_turns_itself_off_after_five_failures_in_a_row_and_a_success_resets_the_count()
    {
        var state = TriggerState.For(Guid.NewGuid(), "cold");
        var now = DateTimeOffset.UnixEpoch;

        for (var i = 0; i < 4; i++)
        {
            Assert.False(state.Failed("boom", now));
        }

        state.Succeeded(now);
        Assert.Equal(0, state.ConsecutiveFailures);
        Assert.Null(state.LastError);

        for (var i = 0; i < 4; i++)
        {
            Assert.False(state.Failed("boom", now));
        }

        Assert.True(state.Failed("boom", now));
        Assert.False(state.Enabled);
        Assert.True(state.DisabledByFailures);
        Assert.False(state.Failed("again", now));

        state.Enable();
        Assert.True(state.Enabled);
        Assert.Equal(0, state.ConsecutiveFailures);
        Assert.False(state.DisabledByFailures);
    }

    [Fact]
    public void An_admin_can_turn_a_trigger_off_without_it_counting_as_a_failure_stop()
    {
        var state = TriggerState.For(Guid.NewGuid(), "cold");

        state.Disable();

        Assert.False(state.Enabled);
        Assert.False(state.DisabledByFailures);
    }
}

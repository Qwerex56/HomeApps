using HomeMix.Domain.Devices;
using HomeMix.Domain.Manifests;
using HomeMix.Domain.Triggers;

namespace HomeMix.Domain.Tests;

public sealed class TriggerMatcherTests
{
    private static readonly DeviceCandidate Station = new(Guid.NewGuid(), "Stacja", true, null, [], ["weather"]);

    private static AddonTrigger Trigger(string field) =>
        new("t", TriggerKind.State, new DeviceMatch("weather", null, null, null), field, null, "triggers/t.js");

    [Theory]
    [InlineData("temperature", "temperature", true)]
    [InlineData("temperature", "seat.temperature", true)]
    [InlineData("temp", "seat.temperature", false)]
    [InlineData("temp", "temperature", false)]
    [InlineData("perature", "temperature", false)]
    public void A_field_matches_exactly_or_after_an_instance_dot(string field, string changed, bool expected) =>
        Assert.Equal(expected, TriggerMatcher.MatchesState(Trigger(field), Station, [changed]));
}

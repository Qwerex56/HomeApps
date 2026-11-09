using Notifications.Domain.Recipients;

namespace Notifications.Domain.Tests;

public sealed class DeliveryTests
{
    private static readonly Guid Household = Guid.NewGuid();

    [Fact]
    public void Without_devices_nothing_is_delivered() =>
        Assert.Equal(DeliveryDecision.NoDevices, Recipient.Create(Guid.NewGuid()).Decide(Household, "chat", A.Now));

    [Fact]
    public void A_muted_household_gets_nothing_from_any_source()
    {
        var recipient = A.RecipientWithDevice();
        recipient.Mute(Household, null);

        Assert.Equal(DeliveryDecision.Muted, recipient.Decide(Household, "chat", A.Now));
        Assert.Equal(DeliveryDecision.Muted, recipient.Decide(Household, "todo", A.Now));
        Assert.Equal(DeliveryDecision.Deliver, recipient.Decide(Guid.NewGuid(), "chat", A.Now));
        Assert.Equal(DeliveryDecision.Deliver, recipient.Decide(null, "chat", A.Now));
    }

    [Fact]
    public void A_muted_plugin_mutes_only_that_plugin_in_that_household()
    {
        var recipient = A.RecipientWithDevice();
        recipient.Mute(Household, "chat");
        recipient.Mute(Household, " chat ");

        Assert.Single(recipient.Mutes);
        Assert.Equal(DeliveryDecision.Muted, recipient.Decide(Household, "chat", A.Now));
        Assert.Equal(DeliveryDecision.Deliver, recipient.Decide(Household, "todo", A.Now));

        recipient.Unmute(Household, "chat");
        Assert.Equal(DeliveryDecision.Deliver, recipient.Decide(Household, "chat", A.Now));
    }

    [Fact]
    public void A_mute_needs_a_household() =>
        Assert.Equal("notifications.invalid_mute", A.RecipientWithDevice().Mute(Guid.Empty, null).Code());

    [Theory]
    [InlineData(21, 59, DeliveryDecision.Deliver)]
    [InlineData(22, 0, DeliveryDecision.QuietHours)]
    [InlineData(3, 0, DeliveryDecision.QuietHours)]
    [InlineData(7, 0, DeliveryDecision.Deliver)]
    public void Quiet_hours_over_midnight_in_the_persons_time_zone(int hour, int minute, DeliveryDecision expected)
    {
        var recipient = A.RecipientWithDevice();
        recipient.ChangeSettings(true, QuietHours.Create(new TimeOnly(22, 0), new TimeOnly(7, 0), "Europe/Warsaw").Value);

        // November: Warsaw is UTC+1.
        var now = new DateTimeOffset(2026, 11, 9, hour, minute, 0, TimeSpan.FromHours(1));

        Assert.Equal(expected, recipient.Decide(Household, "chat", now));
    }

    [Fact]
    public void Quiet_hours_within_one_day()
    {
        var quiet = QuietHours.Create(new TimeOnly(13, 0), new TimeOnly(15, 0), "UTC").Value;

        Assert.True(quiet.Covers(A.Now.AddHours(1)));
        Assert.False(quiet.Covers(A.Now));
        Assert.False(quiet.Covers(A.Now.AddHours(3)));
    }

    [Fact]
    public void Quiet_hours_need_two_times_and_a_known_zone()
    {
        Assert.Equal("notifications.quiet_hours_empty", QuietHours.Create(new TimeOnly(1, 0), new TimeOnly(1, 0), "UTC").Code());
        Assert.Equal("notifications.unknown_time_zone", QuietHours.Create(new TimeOnly(1, 0), new TimeOnly(2, 0), "Mars/Olympus").Code());
    }
}

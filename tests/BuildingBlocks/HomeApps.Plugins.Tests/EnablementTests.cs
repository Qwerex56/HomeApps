namespace HomeApps.Plugins.Tests;

public sealed class EnablementTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_household_has_it_off()
    {
        var enablement = PluginEnablement.For(Guid.NewGuid());

        Assert.False(enablement.Enabled);
        Assert.Null(enablement.ChangedBy);
        Assert.Empty(enablement.DomainEvents);
    }

    [Fact]
    public void Only_the_first_switch_on_is_the_first_time()
    {
        var household = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var enablement = PluginEnablement.For(household);

        enablement.Switch(true, admin, Now);
        enablement.Switch(false, admin, Now.AddDays(1));
        enablement.Switch(true, admin, Now.AddDays(2));

        Assert.Equal(
            [new PluginSwitchedOn(household, admin, true), new PluginSwitchedOff(household, admin), new PluginSwitchedOn(household, admin, false)],
            enablement.DomainEvents);
        Assert.Equal(Now, enablement.FirstEnabledAt);
        Assert.Equal(Now.AddDays(2), enablement.ChangedAt);
    }

    [Fact]
    public void Switching_to_the_same_state_changes_nothing()
    {
        var enablement = PluginEnablement.For(Guid.NewGuid());

        enablement.Switch(false, Guid.NewGuid(), Now);

        Assert.Empty(enablement.DomainEvents);
        Assert.Null(enablement.ChangedAt);
    }
}

using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;

namespace Jarvis.Api.Tests;

/// <summary>H-2, D1: what the household's JARVIS settings let through, by the kind of the source.</summary>
public sealed class HouseholdCeilingTests
{
    private static ToolSource Source(ToolSourceKind kind) => new(kind.ToString(), kind.ToString(), new Uri("http://x/mcp"), kind);

    [Theory]
    [InlineData("homemix", ToolSourceKind.HomeMix)]
    [InlineData("devices", ToolSourceKind.Devices)]
    [InlineData("households", ToolSourceKind.Core)]
    [InlineData("accounts", ToolSourceKind.Core)]
    public void A_configured_source_is_mapped_by_name(string name, ToolSourceKind kind) =>
        Assert.Equal(kind, ConfiguredToolSources.KindOf(name));

    [Fact]
    public void Only_the_allowed_sources_pass_and_the_core_ones_always()
    {
        var ceiling = HouseholdCeiling.From(true, "Member", "Plugins, Devices");

        Assert.True(ceiling.Allows(Source(ToolSourceKind.Plugins)));
        Assert.True(ceiling.Allows(Source(ToolSourceKind.Devices)));
        Assert.False(ceiling.Allows(Source(ToolSourceKind.HomeMix)));
        Assert.True(ceiling.Allows(Source(ToolSourceKind.Core)));
        Assert.False(ceiling.ReadOnly);
    }

    [Theory]
    [InlineData("All")]
    [InlineData("all")]
    [InlineData("Plugins, All")]
    public void All_allows_every_source(string sources)
    {
        var ceiling = HouseholdCeiling.From(true, "Member", sources);

        Assert.True(ceiling.Allows(Source(ToolSourceKind.Plugins)));
        Assert.True(ceiling.Allows(Source(ToolSourceKind.HomeMix)));
        Assert.True(ceiling.Allows(Source(ToolSourceKind.Devices)));
    }

    [Fact]
    public void Unavailable_is_as_closed_as_closed_but_distinct()
    {
        Assert.False(HouseholdCeiling.Unavailable.Allows(Source(ToolSourceKind.Plugins)));
        Assert.True(HouseholdCeiling.Unavailable.ReadOnly);
        Assert.True(HouseholdCeiling.Unavailable.SettingsUnavailable);
        Assert.False(HouseholdCeiling.Closed.SettingsUnavailable);
    }

    [Fact]
    public void A_disabled_assistant_leaves_only_the_core_sources()
    {
        var ceiling = HouseholdCeiling.From(false, "Member", "Plugins, HomeMix, Devices");

        Assert.All([ToolSourceKind.Plugins, ToolSourceKind.HomeMix, ToolSourceKind.Devices], k => Assert.False(ceiling.Allows(Source(k))));
        Assert.True(ceiling.Allows(Source(ToolSourceKind.Core)));
    }

    [Theory]
    [InlineData("ReadOnly", true)]
    [InlineData("Member", false)]
    [InlineData(null, true)]
    public void Read_only_is_everything_but_the_member_level(string? level, bool readOnly) =>
        Assert.Equal(readOnly, HouseholdCeiling.From(true, level, "None").ReadOnly);

    [Fact]
    public void A_household_that_cannot_be_read_is_closed()
    {
        Assert.False(HouseholdCeiling.Closed.Enabled);
        Assert.True(HouseholdCeiling.Closed.ReadOnly);
        Assert.False(HouseholdCeiling.Closed.Allows(Source(ToolSourceKind.Plugins)));
    }
}

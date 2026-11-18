using Households.Domain.Common;
using Households.Domain.Households;

namespace Households.Domain.Tests;

public sealed class AssistantTests
{
    private readonly FixedTime _time = new(A.Now);

    private static Actor Stranger => A.User(Guid.NewGuid());

    private static FluentResults.Result Update(AHousehold h, string who, string? name = "Jarvis", bool enabled = true,
        AssistantAccessLevel level = AssistantAccessLevel.Member, AssistantToolSources sources = AssistantToolSources.Plugins,
        int events = 60, int location = 14) =>
        h.Household.UpdateAssistant(h.Of(who), enabled, name, level, sources, events, location, h.Time);

    [Fact]
    public void EveryHouseholdStartsWithTheDefaultAssistantAndItIsNotAMember()
    {
        var household = Household.Create(Guid.NewGuid(), Guid.NewGuid(), "Dom", null, _time).Value;

        var a = household.Assistant;
        Assert.Equal((true, "JARVIS", AssistantAccessLevel.ReadOnly, AssistantToolSources.All, 30, 7),
            (a.Enabled, a.Name, a.AccessLevel, a.ToolSources, a.EventDays, a.LocationEventDays));
        Assert.Single(household.Members);
        Assert.Equal(HouseholdRole.Guest, household.AssistantRole().Value);
    }

    [Theory]
    [InlineData("owner", true)]
    [InlineData("admin", true)]
    [InlineData("member", false)]
    [InlineData("guest", false)]
    public void OnlyOwnerAndAdminChangeIt(string who, bool allowed)
    {
        var h = new AHousehold(_time);

        var result = Update(h, who);

        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? "Jarvis" : "JARVIS", h.Household.Assistant.Name);
        if (!allowed)
        {
            Assert.Equal("household.forbidden", result.Code());
        }
    }

    [Fact]
    public void AStrangerGetsNotFoundAndNothingChanges()
    {
        var h = new AHousehold(_time);

        var result = h.Household.UpdateAssistant(Stranger, false, "X", AssistantAccessLevel.Member, AssistantToolSources.None, 5, 1, _time);

        Assert.Equal("household.not_found", result.Code());
        Assert.True(h.Household.Assistant.Enabled);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("admin")]
    [InlineData("member")]
    [InlineData("guest")]
    public void EveryoneWithAccessMayLookAndAStrangerMayNot(string who)
    {
        var h = new AHousehold(_time);

        Assert.True(h.Household.ViewAssistant(h.Of(who), _time.Now).IsSuccess);
        Assert.Equal("household.not_found", h.Household.ViewAssistant(Stranger, _time.Now).Code());
    }

    [Fact]
    public void AllSettingsAreStoredAndTheNameIsTrimmed()
    {
        var h = new AHousehold(_time);

        Assert.True(Update(h, "admin", "  Jarek ", false, AssistantAccessLevel.Member, AssistantToolSources.Devices | AssistantToolSources.HomeMix, 90, 30).IsSuccess);

        var a = h.Household.Assistant;
        Assert.Equal((false, "Jarek", AssistantAccessLevel.Member, AssistantToolSources.Devices | AssistantToolSources.HomeMix, 90, 30),
            (a.Enabled, a.Name, a.AccessLevel, a.ToolSources, a.EventDays, a.LocationEventDays));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("0123456789012345678901234567890123")]
    [InlineData("bad\nname")]
    public void ANameMustBeShortTextWithoutControlCharacters(string? name)
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.assistant_name_invalid", Update(h, "owner", name).Code());
        Assert.Equal("JARVIS", h.Household.Assistant.Name);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(366, 7)]
    [InlineData(30, 0)]
    [InlineData(30, 31)]
    [InlineData(400, 91)]
    public void RetentionStaysInBounds(int events, int location)
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.assistant_retention_invalid", Update(h, "owner", events: events, location: location).Code());
        Assert.Equal((30, 7), (h.Household.Assistant.EventDays, h.Household.Assistant.LocationEventDays));
    }

    [Fact]
    public void UnknownLevelOrSourcesAreRefused()
    {
        var h = new AHousehold(_time);

        Assert.Equal("household.assistant_settings_invalid", Update(h, "owner", level: (AssistantAccessLevel)5).Code());
        Assert.Equal("household.assistant_settings_invalid", Update(h, "owner", sources: (AssistantToolSources)64).Code());
    }

    [Theory]
    [InlineData(AssistantAccessLevel.ReadOnly, HouseholdRole.Guest)]
    [InlineData(AssistantAccessLevel.Member, HouseholdRole.Member)]
    public void TheLevelMapsToARoleAndNeverToAdmin(AssistantAccessLevel level, HouseholdRole role)
    {
        var h = new AHousehold(_time);
        Update(h, "owner", level: level);

        Assert.Equal(role, h.Household.AssistantRole().Value);
    }

    [Fact]
    public void ADisabledAssistantOrAnArchivedHouseholdGivesNoRole()
    {
        var h = new AHousehold(_time);
        Update(h, "owner", enabled: false);
        Assert.Equal("household.not_found", h.Household.AssistantRole().Code());

        Update(h, "owner", enabled: true);
        Assert.True(h.Household.Archive(h.Of("owner"), _time).IsSuccess);
        Assert.Equal("household.not_found", h.Household.AssistantRole().Code());
    }
}

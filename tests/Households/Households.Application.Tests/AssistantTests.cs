using Households.Application.Assistant;
using Households.Application.Assistant.GetAssistant;
using Households.Application.Assistant.UpdateAssistant;
using Households.Application.Households;
using Households.Application.Members;
using Households.Domain.Households;

namespace Households.Application.Tests;

public sealed class AssistantTests
{
    private static UpdateAssistantCommand Change(Guid id, bool enabled = true, AssistantAccessLevel level = AssistantAccessLevel.ReadOnly, string name = "Jarvis") =>
        new(id, enabled, name, level, AssistantToolSources.Plugins, 14, 3);

    [Fact]
    public async Task EveryoneWithAccessReadsTheDefaultsGuestsToo()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Guest);

        var view = (await w.Send(new GetAssistantQuery(f.Id))).Value;

        Assert.Equal(new AssistantView(true, "JARVIS", AssistantAccessLevel.ReadOnly, AssistantToolSources.All, 30, 7), view);
        w.SignInAs(Guid.NewGuid());
        Assert.Equal("household.not_found", (await w.Send(new GetAssistantQuery(f.Id))).Code());
    }

    [Fact]
    public async Task AdminChangesItAndTheNewSettingsAreReturned()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Admin);

        var view = (await w.Send(Change(f.Id, level: AssistantAccessLevel.Member))).Value;

        Assert.Equal(new AssistantView(true, "Jarvis", AssistantAccessLevel.Member, AssistantToolSources.Plugins, 14, 3), view);
        Assert.Equal(view, (await w.Send(new GetAssistantQuery(f.Id))).Value);
        Assert.Equal(1, w.Household(f.Id).Members.Count(m => m.Role == HouseholdRole.Owner));
    }

    [Fact]
    public async Task MemberAndGuestMayNotChangeIt()
    {
        using var w = new World();
        var f = await w.Family();

        foreach (var who in new[] { f.Member, f.Guest })
        {
            w.SignInAs(who);
            Assert.Equal("household.forbidden", (await w.Send(Change(f.Id))).Code());
        }
    }

    [Fact]
    public async Task InvalidSettingsAreRefused()
    {
        using var w = new World();
        var f = await w.Family();

        Assert.Equal("household.assistant_name_invalid", (await w.Send(Change(f.Id, name: " "))).Code());
        Assert.True((await w.Send(Change(f.Id, level: (AssistantAccessLevel)9))).IsFailed);
    }

    [Theory]
    [InlineData(AssistantAccessLevel.ReadOnly, HouseholdRole.Guest)]
    [InlineData(AssistantAccessLevel.Member, HouseholdRole.Member)]
    public async Task ASatelliteIsTheAssistantWithTheRoleOfItsLevel(AssistantAccessLevel level, HouseholdRole role)
    {
        using var w = new World();
        var f = await w.Family();
        await w.Send(Change(f.Id, level: level));
        w.SignInAs(Guid.NewGuid());
        w.CurrentUser.SatelliteHouseholdId = f.Id;

        var me = (await w.Send(new GetMyMembershipQuery(f.Id))).Value;

        Assert.Equal((role, true, "Jarvis"), (me.Role, me.IsAssistant, me.Nickname));
    }

    [Fact]
    public async Task ASatelliteIsAStrangerWhenTheAssistantIsOffTheHouseholdIsArchivedOrItIsAnotherHousehold()
    {
        using var w = new World();
        var f = await w.Family();
        var other = await w.Family("Inny");
        w.SignInAs(f.Owner);
        await w.Send(Change(f.Id, enabled: false));
        w.SignInAs(Guid.NewGuid());
        w.CurrentUser.SatelliteHouseholdId = f.Id;
        Assert.Equal("household.not_found", (await w.Send(new GetMyMembershipQuery(f.Id))).Code());

        w.SignInAs(f.Owner);
        await w.Send(Change(f.Id));
        await w.Send(new ArchiveHouseholdCommand(f.Id));
        w.SignInAs(Guid.NewGuid());
        Assert.Equal("household.not_found", (await w.Send(new GetMyMembershipQuery(f.Id))).Code());

        w.CurrentUser.SatelliteHouseholdId = f.Id;
        Assert.Equal("household.not_found", (await w.Send(new GetMyMembershipQuery(other.Id))).Code());
    }

    [Fact]
    public async Task APersonIsNotTheAssistant()
    {
        using var w = new World();
        var f = await w.Family();
        w.SignInAs(f.Member);

        var me = (await w.Send(new GetMyMembershipQuery(f.Id))).Value;

        Assert.Equal((HouseholdRole.Member, false), (me.Role, me.IsAssistant));
    }
}

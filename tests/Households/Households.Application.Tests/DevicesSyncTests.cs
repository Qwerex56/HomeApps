using Households.Application.DevicesSync.AddDeviceMember;
using Households.Application.DevicesSync.RemoveDeviceMember;
using Households.Application.Members;
using Households.Domain.Households;
using Contracts = HomeApps.Contracts.Households;

namespace Households.Application.Tests;

/// <summary>jarvis-satellites.md 3a (S13): Households follows Devices' pairing of household satellites.</summary>
public sealed class DevicesSyncTests
{
    [Fact]
    public async Task A_paired_satellite_is_listed_as_a_JARVIS_member_and_leaves_when_unpaired()
    {
        using var w = new World();
        var family = await w.Family();
        var device = Guid.NewGuid();

        Assert.True((await w.Send(new AddDeviceMemberCommand(device, family.Id, "salon"))).IsSuccess);
        Assert.True((await w.Send(new AddDeviceMemberCommand(device, family.Id, "salon"))).IsSuccess);
        var members = (await w.Send(new ListMembersQuery(family.Id))).Value;

        var satellite = Assert.Single(members, m => m.IsDevice);
        Assert.Equal(device, satellite.UserId);
        Assert.Equal("JARVIS · salon", satellite.Name);
        Assert.Equal(HouseholdRole.Member, satellite.Role);
        Assert.Single(w.Bus.Published.OfType<Contracts.MemberJoined>());

        await w.Send(new RemoveDeviceMemberCommand(device, family.Id));

        Assert.Null(w.Household(family.Id).MemberOf(device));
    }

    [Fact]
    public async Task A_satellite_of_an_unknown_household_is_dropped()
    {
        using var w = new World();

        Assert.True((await w.Send(new AddDeviceMemberCommand(Guid.NewGuid(), Guid.NewGuid(), "salon"))).IsSuccess);
        Assert.True((await w.Send(new RemoveDeviceMemberCommand(Guid.NewGuid(), Guid.NewGuid()))).IsSuccess);
    }
}

using Devices.Application.HouseholdDevices.NewPairingCode;
using Devices.Application.MyDevices.RemoveDevice;
using Devices.Application.Tests.Fakes;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests;

/// <summary>H-4c item 4 (pinning): removing a device or giving it a new code closes its live link.</summary>
public sealed class LinkDropTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _admin = Guid.NewGuid();
    private readonly Guid _household = Guid.NewGuid();
    private readonly FakeDevices _devices = new();
    private readonly FakeLinks _links = new();
    private readonly FakeBus _bus = new();
    private readonly FakeTime _time = new(Now);
    private readonly Device _device;

    public LinkDropTests()
    {
        _device = PairedDevice.Create(Now, householdId: _household);
        _devices.Add(_device);
    }

    [Fact]
    public async Task Removing_a_device_drops_its_link()
    {
        var result = await new RemoveDeviceHandler(_devices, new FakeRoles(HouseholdRole.Admin, _household), _links, _bus, new FakeUser(_admin), _time)
            .Handle(new RemoveDeviceCommand(_device.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([_device.Id], _links.Dropped);
        Assert.True(_device.IsDisabled);
    }

    [Fact]
    public async Task A_new_pairing_code_drops_the_link_of_the_old_hardware()
    {
        var result = await new NewPairingCodeHandler(_devices, new FakeRoles(HouseholdRole.Admin, _household), _links, _bus, new FakeUser(_admin), _time)
            .Handle(new NewPairingCodeCommand(_device.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([_device.Id], _links.Dropped);
    }

    [Fact]
    public async Task A_member_who_may_not_manage_the_device_does_not_drop_its_link()
    {
        var result = await new RemoveDeviceHandler(_devices, new FakeRoles(HouseholdRole.Member, _household), _links, _bus, new FakeUser(_admin), _time)
            .Handle(new RemoveDeviceCommand(_device.Id), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.Empty(_links.Dropped);
    }

    [Fact]
    public void A_removed_device_cannot_connect_again()
    {
        _device.Disable(Now);

        Assert.Equal("devices.disabled", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)_device.Connect([], Now).Errors[0]).Code);
    }

    [Fact]
    public void A_device_with_a_new_pairing_code_cannot_connect_until_it_is_paired_again()
    {
        _device.NewPairingCode("new-hash", Now);

        Assert.Equal("devices.not_paired", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)_device.Connect([], Now).Errors[0]).Code);
    }
}

using Devices.Domain.Devices;

namespace Devices.Domain.Tests;

public sealed class DeviceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 1, 0, 0, TimeSpan.FromHours(1));

    [Theory]
    [InlineData("mac-biuro")]
    [InlineData("c3")]
    public void A_short_lowercase_name_is_fine(string name) => Assert.True(Device.Create(Guid.NewGuid(), name, DeviceKind.Computer, Now).IsSuccess);

    [Theory]
    [InlineData("")]
    [InlineData("Mac")]
    [InlineData("mac_biuro")]
    [InlineData("-mac")]
    [InlineData("a-name-longer-than-twenty")]
    public void Other_names_are_refused(string name) =>
        Assert.Equal("devices.invalid_name", Code(Device.Create(Guid.NewGuid(), name, DeviceKind.Computer, Now).Errors));

    [Theory]
    [InlineData("Say", null)]
    [InlineData("say", "[]")]
    [InlineData("say", "not json")]
    public void An_action_needs_a_snake_case_name_and_an_object_schema(string name, string? schema) =>
        Assert.Equal("devices.invalid_action", Code(DeviceAction.Create(name, "x", schema, false, false).Errors));

    [Fact]
    public void Nie_pytaj_stays_when_the_device_announces_the_action_again()
    {
        var device = Device.Create(Guid.NewGuid(), "mac", DeviceKind.Computer, Now).Value;
        device.Connect([Action("lights", confirm: true)], Now);
        Assert.True(device.Find("lights")!.NeedsConfirmation);

        device.AllowInAdvance("lights", allowed: true);
        device.Connect([Action("lights", confirm: true), Action("say")], Now.AddMinutes(5));

        Assert.False(device.Find("lights")!.NeedsConfirmation);
        Assert.False(device.Find("say")!.AutoApprove);
        Assert.Equal(Now.AddMinutes(5), device.LastSeenAt);
    }

    [Fact]
    public void Only_a_marked_change_needs_a_confirmation()
    {
        Assert.False(Action("read", confirm: true, readOnly: true).NeedsConfirmation);
        Assert.False(Action("beep").NeedsConfirmation);
        Assert.True(Action("heat", confirm: true).NeedsConfirmation);
    }

    [Fact]
    public void The_same_action_twice_is_refused()
    {
        var device = Device.Create(Guid.NewGuid(), "mac", DeviceKind.Computer, Now).Value;

        Assert.Equal("devices.too_many_actions", Code(device.Connect([Action("say"), Action("say")], Now).Errors));
    }

    [Fact]
    public void A_removed_device_cannot_connect_until_restored()
    {
        var device = Device.Create(Guid.NewGuid(), "mac", DeviceKind.Computer, Now).Value;
        device.Disable(Now);

        Assert.Equal("devices.disabled", Code(device.Connect([], Now).Errors));

        device.Enable();
        Assert.True(device.Connect([], Now).IsSuccess);
    }

    [Fact]
    public void The_tool_name_is_the_device_and_the_action() =>
        Assert.Equal("mac-biuro_say", Device.Create(Guid.NewGuid(), "mac-biuro", DeviceKind.Computer, Now).Value.ToolName(Action("say")));

    private static DeviceAction Action(string name, bool confirm = false, bool readOnly = false) =>
        DeviceAction.Create(name, "Does " + name, null, readOnly, confirm).Value;

    private static string? Code(IEnumerable<FluentResults.IError> errors) =>
        errors.OfType<HomeApps.BuildingBlocks.Domain.Errors.AppError>().FirstOrDefault()?.Code;
}

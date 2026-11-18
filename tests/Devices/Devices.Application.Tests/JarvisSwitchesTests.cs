using Devices.Application.HouseholdDevices.SetJarvisAccess;
using Devices.Application.Tests.Fakes;
using Devices.Application.Tools.CallTool;
using Devices.Application.Tools.ListTools;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;

namespace Devices.Application.Tests;

/// <summary>H-3d (D6): "JARVIS może sterować" and "Nie pytaj" per household device.</summary>
public sealed class JarvisSwitchesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _user = Guid.NewGuid();
    private readonly Guid _household = Guid.NewGuid();
    private readonly FakeDevices _devices = new();
    private readonly FakeLinks _links = new();

    private Device Chair(bool mayControl = true, bool asks = true)
    {
        var device = PairedDevice.Create(Now, householdId: _household);
        device.Connect(
            [DeviceAction.Create("say", null, null, false, false).Value, DeviceAction.Create("peek", null, null, true, false).Value], Now);
        device.SetJarvisAccess(mayControl, asks);
        _devices.Add(device);
        return device;
    }

    private async Task<Dictionary<string, Devices.Application.Common.DeviceTool>> ListAsync(bool jarvis) =>
        (await new ListToolsHandler(_devices, new FakeRoles(HouseholdRole.Member, _household), _links, new FakeUser(_user, jarvis))
            .Handle(new ListToolsQuery(), CancellationToken.None)).Value.ToDictionary(t => t.Name);

    private readonly FakeSender _sender = new();

    private Task<FluentResults.Result<string>> CallAsync(string tool, bool jarvis, System.Text.Json.JsonElement? input = null) =>
        new CallToolHandler(_devices, new FakeRoles(HouseholdRole.Member, _household), _links, new FakeUser(_user, jarvis), _sender)
            .Handle(new CallToolCommand(tool, input), CancellationToken.None);

    [Fact]
    public async Task By_default_JARVIS_may_control_and_asks_first()
    {
        Chair();

        var tools = await ListAsync(jarvis: true);

        Assert.True(tools["fotel_set"].NeedsConfirmation);
        Assert.True(tools["fotel_say"].NeedsConfirmation);
        Assert.False(tools["fotel_peek"].NeedsConfirmation);
        Assert.False(tools["fotel_state"].NeedsConfirmation);
    }

    [Fact]
    public async Task Nie_pytaj_marks_set_and_actions_so_JARVIS_runs_them_without_asking()
    {
        Chair(asks: false);

        var tools = await ListAsync(jarvis: true);

        Assert.False(tools["fotel_set"].NeedsConfirmation);
        Assert.False(tools["fotel_say"].NeedsConfirmation);
    }

    [Fact]
    public async Task Without_permission_JARVIS_sees_neither_set_nor_changing_actions_but_still_reads()
    {
        Chair(mayControl: false);

        var tools = await ListAsync(jarvis: true);

        Assert.DoesNotContain("fotel_set", tools.Keys);
        Assert.DoesNotContain("fotel_say", tools.Keys);
        Assert.Contains("fotel_peek", tools.Keys);
        Assert.Contains("fotel_state", tools.Keys);
        Assert.Contains("fotel_events", tools.Keys);
    }

    [Fact]
    public async Task A_person_still_sees_every_tool_when_JARVIS_may_not_control()
    {
        Chair(mayControl: false);

        var tools = await ListAsync(jarvis: false);

        Assert.Contains("fotel_set", tools.Keys);
        Assert.Contains("fotel_say", tools.Keys);
    }

    [Fact]
    public async Task JARVIS_is_refused_set_and_changing_actions_without_permission()
    {
        Chair(mayControl: false);

        foreach (var tool in new[] { "fotel_set", "fotel_say" })
        {
            var result = await CallAsync(tool, jarvis: true);
            Assert.Equal("devices.jarvis_not_allowed", ((HomeApps.BuildingBlocks.Domain.Errors.AppError)result.Errors.Single()).Code);
        }

        Assert.Empty(_links.Runs);
    }

    [Fact]
    public async Task A_person_may_run_an_action_even_when_JARVIS_may_not()
    {
        Chair(mayControl: false);

        Assert.True((await CallAsync("fotel_say", jarvis: false)).IsSuccess);
        Assert.True((await CallAsync("fotel_peek", jarvis: true)).IsSuccess);
        Assert.Equal(["say", "peek"], _links.Runs.Select(r => r.Action));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Set_goes_through_SetControl_when_JARVIS_may_control_and_for_a_person(bool jarvis)
    {
        var device = Chair(mayControl: jarvis ? true : false);
        using var input = System.Text.Json.JsonDocument.Parse("""{"control":"light","value":true}""");

        var result = await CallAsync("fotel_set", jarvis, input.RootElement.Clone());

        Assert.True(result.IsSuccess);
        Assert.Equal("Done.", result.Value);
        var sent = Assert.IsType<Devices.Application.Controls.SetControl.SetControlCommand>(Assert.Single(_sender.Sent));
        Assert.Equal(device.Id, sent.DeviceId);
        Assert.Equal("light", sent.Control);
        Assert.Equal(System.Text.Json.JsonValueKind.True, sent.Value!.Value.ValueKind);
    }

    [Fact]
    public async Task A_refused_set_never_reaches_SetControl()
    {
        Chair(mayControl: false);

        Assert.True((await CallAsync("fotel_set", jarvis: true)).IsFailed);
        Assert.Empty(_sender.Sent);
    }

    private SetJarvisAccessHandler Handler(HouseholdRole? role) => new(_devices, new FakeRoles(role, _household), new FakeUser(_user));

    [Fact]
    public async Task An_admin_sets_the_switches()
    {
        var device = Chair();

        var result = await Handler(HouseholdRole.Admin).Handle(new SetJarvisAccessCommand(device.Id, false, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(device.JarvisMayControl);
        Assert.False(device.JarvisAsks);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task A_member_or_guest_cannot(HouseholdRole role)
    {
        var device = Chair();

        var result = await Handler(role).Handle(new SetJarvisAccessCommand(device.Id, false, false), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.True(device.JarvisMayControl);
    }

    [Fact]
    public async Task A_personal_device_has_no_switches()
    {
        var own = Device.Create(_user, "Mac", DeviceKind.Computer, [], Now).Value;
        _devices.Add(own);

        var result = await Handler(HouseholdRole.Admin).Handle(new SetJarvisAccessCommand(own.Id, false, false), CancellationToken.None);

        Assert.True(result.IsFailed);
    }

    [Fact]
    public void The_validator_wants_a_device()
    {
        Assert.False(new SetJarvisAccessValidator().Validate(new SetJarvisAccessCommand(Guid.Empty, true, true)).IsValid);
        Assert.True(new SetJarvisAccessValidator().Validate(new SetJarvisAccessCommand(Guid.NewGuid(), true, true)).IsValid);
    }
}

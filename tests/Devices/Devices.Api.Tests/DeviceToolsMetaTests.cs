using Devices.Api.Mcp;
using Devices.Application.Common;

namespace Devices.Api.Tests;

/// <summary>H-4c item 6: the MCP tool of a person's computer says so in <c>_meta</c>; a household device's tool names its household and nothing else. No database needed.</summary>
public sealed class DeviceToolsMetaTests
{
    private static readonly Guid Household = Guid.NewGuid();

    private static async Task<Dictionary<string, ModelContextProtocol.Protocol.Tool>> ListAsync(params DeviceTool[] tools) =>
        (await new DeviceToolsServer(new FakeToolSender(tools)).ListToolsAsync(CancellationToken.None)).Tools.ToDictionary(t => t.Name);

    [Fact]
    public async Task A_computer_tool_has_the_purpose_and_no_household()
    {
        var tools = await ListAsync(new DeviceTool("mac_open_app", "x", null, false, true, null, OnComputer: true));

        var meta = tools["mac_open_app"].Meta!;
        Assert.Equal("computer", meta["homeapps/purpose"]!.GetValue<string>());
        Assert.False(meta.ContainsKey("homeapps/household"));
    }

    [Fact]
    public async Task A_household_tool_has_the_household_and_no_purpose()
    {
        var tools = await ListAsync(new DeviceTool("fotel_say", "x", null, false, true, Household));

        var meta = tools["fotel_say"].Meta!;
        Assert.Equal(Household.ToString(), meta["homeapps/household"]!.GetValue<string>());
        Assert.False(meta.ContainsKey("homeapps/purpose"));
    }

    [Fact]
    public async Task A_tool_of_a_persons_own_non_computer_device_has_no_meta()
    {
        var tools = await ListAsync(new DeviceTool("x_say", "x", null, false, false));

        Assert.Null(tools["x_say"].Meta);
    }
}

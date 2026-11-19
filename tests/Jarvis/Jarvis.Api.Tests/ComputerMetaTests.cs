using System.Text.Json.Nodes;
using Jarvis.Api.Gateway;
using Jarvis.Application.Gateway;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Tests;

/// <summary>H-4c item 6: only a Devices source says a tool is on a computer; anything else is ignored and never throws.</summary>
public sealed class ComputerMetaTests
{
    private static ToolSource Source(ToolSourceKind kind) => new("src", "src", new Uri("http://src.test/mcp"), kind);

    private static Tool WithPurpose(JsonNode? value) => new() { Name = "t", Meta = new JsonObject { ["homeapps/purpose"] = value } };

    [Fact]
    public void A_Devices_tool_with_the_computer_purpose_is_on_a_computer() =>
        Assert.True(McpGateway.OnComputer(Source(ToolSourceKind.Devices), WithPurpose("computer")));

    [Theory]
    [InlineData(ToolSourceKind.Plugins)]
    [InlineData(ToolSourceKind.HomeMix)]
    [InlineData(ToolSourceKind.Core)]
    public void Any_other_source_is_not_trusted_so_it_cannot_hide_or_mark_a_tool(ToolSourceKind kind) =>
        Assert.False(McpGateway.OnComputer(Source(kind), WithPurpose("computer")));

    [Fact]
    public void Another_purpose_or_a_strange_meta_is_not_a_computer_and_never_throws()
    {
        var devices = Source(ToolSourceKind.Devices);

        Assert.False(McpGateway.OnComputer(devices, WithPurpose("assistant")));
        Assert.False(McpGateway.OnComputer(devices, WithPurpose(42)));
        Assert.False(McpGateway.OnComputer(devices, WithPurpose(new JsonObject())));
        Assert.False(McpGateway.OnComputer(devices, new Tool { Name = "t" }));
        Assert.False(McpGateway.OnComputer(devices, null));
    }
}

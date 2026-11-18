using System.Text.Json.Nodes;
using Jarvis.Api.Gateway;
using Jarvis.Application.Gateway;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Tests;

/// <summary>H-2: only a Devices source names a tool's household in <c>_meta</c>; anything else is ignored and never throws.</summary>
public sealed class HouseholdOfTests
{
    private static readonly Guid Id = Guid.NewGuid();

    private static ToolSource Source(ToolSourceKind kind) => new("src", "src", new Uri("http://src.test/mcp"), kind);

    private static Tool WithMeta(JsonNode? value) =>
        new() { Name = "t", Meta = new JsonObject { ["homeapps/household"] = value } };

    [Fact]
    public void A_Devices_tool_names_its_household() =>
        Assert.Equal(Id, McpGateway.HouseholdOf(Source(ToolSourceKind.Devices), WithMeta(Id.ToString())));

    [Theory]
    [InlineData(ToolSourceKind.Plugins)]
    [InlineData(ToolSourceKind.HomeMix)]
    [InlineData(ToolSourceKind.Core)]
    public void Any_other_source_is_not_trusted(ToolSourceKind kind) =>
        Assert.Null(McpGateway.HouseholdOf(Source(kind), WithMeta(Id.ToString())));

    [Fact]
    public void A_meta_that_is_not_a_string_is_ignored_without_an_exception()
    {
        Assert.Null(McpGateway.HouseholdOf(Source(ToolSourceKind.Devices), WithMeta(42)));
        Assert.Null(McpGateway.HouseholdOf(Source(ToolSourceKind.Devices), WithMeta(new JsonObject())));
        Assert.Null(McpGateway.HouseholdOf(Source(ToolSourceKind.Devices), WithMeta("not-a-guid")));
        Assert.Null(McpGateway.HouseholdOf(Source(ToolSourceKind.Devices), new Tool { Name = "t" }));
        Assert.Null(McpGateway.HouseholdOf(Source(ToolSourceKind.Devices), null));
    }
}

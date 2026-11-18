using System.Text.Json;
using Jarvis.Api.Gateway;

namespace Jarvis.Api.Tests;

/// <summary>H-2: which household a tool call names, from its <c>household</c> or <c>householdId</c> argument.</summary>
public sealed class HouseholdArgumentTests
{
    private static readonly Guid Id = Guid.NewGuid();

    private static Dictionary<string, JsonElement> Args(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

    [Theory]
    [InlineData("household")]
    [InlineData("householdId")]
    public void Either_name_is_read(string name) =>
        Assert.Equal(Id, McpGateway.HouseholdIn(Args($$"""{"{{name}}":"{{Id}}"}""")));

    [Fact]
    public void The_first_name_wins_when_both_are_given()
    {
        var other = Guid.NewGuid();
        Assert.Equal(Id, McpGateway.HouseholdIn(Args($$"""{"householdId":"{{other}}","household":"{{Id}}"}""")));
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{"household":42}""")]
    [InlineData("""{"household":"not-a-guid"}""")]
    [InlineData("""{"household":null}""")]
    [InlineData("""{"name":"x"}""")]
    public void Anything_else_names_no_household(string json) =>
        Assert.Null(McpGateway.HouseholdIn(Args(json)));

    [Fact]
    public void No_arguments_name_no_household() => Assert.Null(McpGateway.HouseholdIn(null));
}

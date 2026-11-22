using HomeApps.BuildingBlocks.Testing;

namespace Jarvis.Api.Tests;

/// <summary>H-4c item 8: the "Pytaj" annotation of a device's tool is read from Devices on every call; other sources keep their five minutes.</summary>
[Collection(nameof(JarvisCollection))]
public sealed class AskFirstGatewayTests : IDisposable
{
    private readonly JarvisFactory _factory;
    private readonly Guid _person = Guid.NewGuid();
    private readonly Guid _household = Guid.NewGuid();

    public AskFirstGatewayTests(JarvisFactory factory)
    {
        _factory = factory;
        _factory.DevicesSource.Reset();
        _factory.ExtraSource.Reset();
    }

    public void Dispose()
    {
        _factory.DevicesSource.Reset();
        _factory.ExtraSource.Reset();
    }

    [Fact]
    public async Task A_device_tool_that_starts_to_ask_is_not_run_on_the_next_call()
    {
        _factory.DevicesSource.Tools = [FakeToolSet.Household("fotel_say", _household, readOnly: false, destructive: false)];
        var answers = new List<string>();
        _factory.Brain.Script = async mcp =>
        {
            answers.Add((await mcp.CallTextAsync("device_fotel_say", new Dictionary<string, object?>())).Text);
            _factory.DevicesSource.Tools = [FakeToolSet.Household("fotel_say", _household, readOnly: false, destructive: true)];
            answers.Add((await mcp.CallTextAsync("device_fotel_say", new Dictionary<string, object?>())).Text);
            return "Gotowe.";
        };

        await AssistantTests.AskAsync(_factory, _person, "powiedz coś");

        Assert.Equal("fotel_say done", answers[0]);
        Assert.StartsWith("Not done yet", answers[1], StringComparison.Ordinal);
        Assert.Equal(["fotel_say"], _factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task A_device_tool_whose_ask_was_switched_off_runs_at_once_on_the_next_call()
    {
        _factory.DevicesSource.Tools = [FakeToolSet.Household("fotel_say", _household, readOnly: false, destructive: true)];
        var answers = new List<string>();
        _factory.Brain.Script = async mcp =>
        {
            answers.Add((await mcp.CallTextAsync("device_fotel_say", new Dictionary<string, object?>())).Text);
            _factory.DevicesSource.Tools = [FakeToolSet.Household("fotel_say", _household, readOnly: false, destructive: false)];
            answers.Add((await mcp.CallTextAsync("device_fotel_say", new Dictionary<string, object?>())).Text);
            return "Gotowe.";
        };

        await AssistantTests.AskAsync(_factory, _person, "powiedz coś");

        Assert.StartsWith("Not done yet", answers[0], StringComparison.Ordinal);
        Assert.Equal("fotel_say done", answers[1]);
        Assert.Equal(["fotel_say"], _factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task A_source_that_is_not_Devices_is_still_cached_so_a_changed_annotation_is_not_seen_at_once()
    {
        _factory.ExtraSource.Tools = [FakeToolSet.Plain("thing", readOnly: false, destructive: false)];
        var answers = new List<string>();
        _factory.Brain.Script = async mcp =>
        {
            answers.Add((await mcp.CallTextAsync("extra_thing", new Dictionary<string, object?>())).Text);
            _factory.ExtraSource.Tools = [FakeToolSet.Plain("thing", readOnly: false, destructive: true)];
            answers.Add((await mcp.CallTextAsync("extra_thing", new Dictionary<string, object?>())).Text);
            return "Gotowe.";
        };

        await AssistantTests.AskAsync(_factory, _person, "zrób to");

        Assert.Equal(["thing done", "thing done"], answers);
        Assert.Equal(["thing", "thing"], _factory.ExtraSource.Calls);
    }
}

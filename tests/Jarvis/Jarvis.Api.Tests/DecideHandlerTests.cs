using HomeApps.Contracts.Assistant;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Actions.Decide;
using Jarvis.Application.Common;
using Jarvis.Domain.Runs;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Api.Tests;

/// <summary>A decision tells the channel of the run, and a card keeps its id whatever its status.</summary>
public sealed class DecideHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeRunRepository _runs = new();
    private readonly FakeToolCallRepository _calls = new();
    private readonly FakeBus _bus = new();
    private readonly Clock _time = new(Now);
    private readonly Guid _person = Guid.NewGuid();

    [Theory]
    [InlineData(ToolCallStatus.Done)]
    [InlineData(ToolCallStatus.Failed)]
    [InlineData(ToolCallStatus.Denied)]
    [InlineData(ToolCallStatus.NeedsConfirmation)]
    public void Cards_keep_the_confirmation_id_for_every_status(ToolCallStatus status)
    {
        var id = Guid.NewGuid();

        var action = Cards.Action(new ActionView(id, "tool", "text", status, null));

        Assert.Equal(id, action.ConfirmationId);
        Assert.Equal(status.ToString(), action.Status);
    }

    [Fact]
    public async Task A_direct_decision_publishes_the_card_to_the_channel_of_the_run()
    {
        var run = AssistantRun.Start(_person, "chat", "c1/m2", null, "q", _time);
        _runs.Add(run);
        var call = ToolCall.AwaitConfirmation(run.Id, _person, "tool", "{}", _time);
        _calls.Add(call);

        var result = await new DecideHandler(_calls, _runs, new Executor(), _bus, _time)
            .Handle(new DecideCommand(_person, call.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var published = Assert.IsType<AssistantResponded>(Assert.Single(_bus.Published));
        Assert.Equal("chat", published.Channel);
        Assert.Equal("c1/m2", published.Reference);
        var action = Assert.Single(published.Actions);
        Assert.Equal(call.Id, action.ConfirmationId);
        Assert.Equal("Done", action.Status);
    }

    [Fact]
    public async Task A_run_without_a_reference_publishes_nothing()
    {
        var run = AssistantRun.Start(_person, "app", null, null, "q", _time);
        _runs.Add(run);
        var call = ToolCall.AwaitConfirmation(run.Id, _person, "tool", "{}", _time);
        _calls.Add(call);

        await new DecideHandler(_calls, _runs, new Executor(), _bus, _time)
            .Handle(new DecideCommand(_person, call.Id, false), CancellationToken.None);

        Assert.Empty(_bus.Published);
    }

    private sealed class Executor : IToolExecutor
    {
        public Task<ToolOutcome> CallAsync(Guid userId, string tool, string? arguments, CancellationToken cancellationToken) =>
            Task.FromResult(new ToolOutcome("ok", false));
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

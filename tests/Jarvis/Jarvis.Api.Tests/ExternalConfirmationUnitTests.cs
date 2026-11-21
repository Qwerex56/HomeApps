using HomeApps.Contracts.Notifications;
using Jarvis.Application.Actions.Decide;
using Jarvis.Application.Actions.GetActionResult;
using Jarvis.Application.Actions.ListPendingConfirmations;
using Jarvis.Application.ToolCalls.AwaitExternalConfirmation;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Api.Tests;

/// <summary>
/// H-7 unit tests: external MCP client awaits confirmation, idempotency, decision outcomes,
/// permissions, and jarvis_action_result.
/// </summary>
public sealed class ExternalConfirmationUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeRunRepository _runs = new();
    private readonly FakeToolCallRepository _calls = new();
    private readonly FakeBus _bus = new();
    private readonly Clock _time = new(Now);
    private readonly Guid _person = Guid.NewGuid();
    private readonly Guid _otherPerson = Guid.NewGuid();

    private AwaitExternalConfirmationHandler CreateAwaitHandler() =>
        new(_runs, _calls, _bus, _time);

    private GetActionResultHandler CreateActionResultHandler() =>
        new(_calls, _time);

    private ListPendingConfirmationsHandler CreateListPendingHandler() =>
        new(_calls, _time);

    [Fact]
    public async Task External_call_creates_pending_action_in_closed_run_and_emits_notification()
    {
        var handler = CreateAwaitHandler();
        var command = new AwaitExternalConfirmationCommand(_person, "claude-code", "device_mac_open_app", "{\"app\":\"Safari\"}", "mac");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var actionId = result.Value;

        var call = Assert.Single(_calls.Calls);
        Assert.Equal(actionId, call.Id);
        Assert.Equal(_person, call.UserId);
        Assert.Equal("device_mac_open_app", call.Tool);
        Assert.Equal(ToolCallStatus.NeedsConfirmation, call.Status);
        Assert.NotNull(call.ConfirmBy);

        var run = Assert.Single(_runs.Runs);
        Assert.Equal(call.RunId, run.Id);
        Assert.Equal("mcp", run.Channel);
        Assert.Equal("claude-code", run.Reference);
        Assert.False(run.IsRunning);

        var notification = Assert.Single(_bus.Published.OfType<NotificationRequested>());
        Assert.Equal([_person], notification.UserIds);
        Assert.Equal("jarvis", notification.Source);
        Assert.Equal("confirmation", notification.Category);
        Assert.Contains("claude-code", notification.Body);
        Assert.Contains("mac_open_app", notification.Body);
        Assert.Contains("mac", notification.Body);
        Assert.Equal("/jarvis", notification.Url);
    }

    [Fact]
    public async Task Repeated_call_with_same_user_tool_and_arguments_returns_the_same_pending_action()
    {
        var handler = CreateAwaitHandler();
        var command1 = new AwaitExternalConfirmationCommand(_person, "claude-code", "device_mac_open_app", "{\"app\":\"Safari\"}", "mac");
        var command2 = new AwaitExternalConfirmationCommand(_person, "other-client", "device_mac_open_app", "{\"app\":\"Safari\"}", "mac");

        var result1 = await handler.Handle(command1, CancellationToken.None);
        var result2 = await handler.Handle(command2, CancellationToken.None);

        Assert.Equal(result1.Value, result2.Value);
        Assert.Single(_calls.Calls);
        Assert.Single(_runs.Runs);
        Assert.Single(_bus.Published);
    }

    [Fact]
    public async Task Action_result_shows_pending_approved_denied_and_expired()
    {
        var awaitHandler = CreateAwaitHandler();
        var actionHandler = CreateActionResultHandler();
        var actionId = (await awaitHandler.Handle(
            new AwaitExternalConfirmationCommand(_person, "client1", "device_mac_open_app", "{}", "mac"), CancellationToken.None)).Value;

        // Pending
        var pendingResult = await actionHandler.Handle(new GetActionResultQuery(_person, actionId), CancellationToken.None);
        Assert.True(pendingResult.IsSuccess);
        Assert.Equal("pending", pendingResult.Value.Status);
        Assert.Null(pendingResult.Value.Text);

        // Stranger cannot see result
        var strangerResult = await actionHandler.Handle(new GetActionResultQuery(_otherPerson, actionId), CancellationToken.None);
        Assert.True(strangerResult.IsFailed);
        Assert.Equal("jarvis.action_not_found", strangerResult.Errors[0].Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey]);

        // Approved
        var call = _calls.Calls.Single(c => c.Id == actionId);
        call.Complete("Safari opened successfully", failed: false);
        var approvedResult = await actionHandler.Handle(new GetActionResultQuery(_person, actionId), CancellationToken.None);
        Assert.Equal("approved", approvedResult.Value.Status);
        Assert.Equal("Safari opened successfully", approvedResult.Value.Text);

        // Denied (new action)
        var actionId2 = (await awaitHandler.Handle(
            new AwaitExternalConfirmationCommand(_person, "client1", "device_mac_close_app", "{}", "mac"), CancellationToken.None)).Value;
        var call2 = _calls.Calls.Single(c => c.Id == actionId2);
        call2.Deny();
        var deniedResult = await actionHandler.Handle(new GetActionResultQuery(_person, actionId2), CancellationToken.None);
        Assert.Equal("denied", deniedResult.Value.Status);

        // Expired (new action, advance clock past 10 minutes)
        var actionId3 = (await awaitHandler.Handle(
            new AwaitExternalConfirmationCommand(_person, "client1", "device_mac_restart", "{}", "mac"), CancellationToken.None)).Value;
        _time.Now += ToolCall.ConfirmationWindow + TimeSpan.FromMinutes(1);
        var expiredResult = await actionHandler.Handle(new GetActionResultQuery(_person, actionId3), CancellationToken.None);
        Assert.Equal("expired", expiredResult.Value.Status);
    }

    [Fact]
    public async Task List_pending_confirmations_returns_only_own_pending_newest_first()
    {
        var awaitHandler = CreateAwaitHandler();
        var listHandler = CreateListPendingHandler();

        var id1 = (await awaitHandler.Handle(new AwaitExternalConfirmationCommand(_person, "c1", "tool1", "{}", "mac"), CancellationToken.None)).Value;
        _time.Now += TimeSpan.FromSeconds(10);
        var id2 = (await awaitHandler.Handle(new AwaitExternalConfirmationCommand(_person, "c1", "tool2", "{}", "mac"), CancellationToken.None)).Value;

        // An action for other person
        await awaitHandler.Handle(new AwaitExternalConfirmationCommand(_otherPerson, "c1", "tool3", "{}", "mac"), CancellationToken.None);

        var list = (await listHandler.Handle(new ListPendingConfirmationsQuery(_person), CancellationToken.None)).Value;

        Assert.Equal(2, list.Count);
        Assert.Equal(id2, list[0].Id);
        Assert.Equal(id1, list[1].Id);
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}

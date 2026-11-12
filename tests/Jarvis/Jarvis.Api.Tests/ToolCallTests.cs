using Jarvis.Domain.ToolCalls;

namespace Jarvis.Api.Tests;

public sealed class ToolCallTests
{
    [Fact]
    public void An_action_left_waiting_past_its_window_expires_and_can_no_longer_be_decided()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var person = Guid.NewGuid();
        var call = ToolCall.AwaitConfirmation(Guid.NewGuid(), person, "todo_delete_task", "{}", clock);

        clock.Now += ToolCall.ConfirmationWindow + TimeSpan.FromSeconds(1);

        Assert.Equal(ToolCallStatus.Expired, call.StatusAt(clock.Now));
        Assert.Equal("jarvis.action_expired", Assert.Single(call.CanDecide(person, clock).Errors).Metadata[HomeApps.BuildingBlocks.Domain.Errors.AppError.CodeKey]);
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}

using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Notifications;
using Jarvis.Application.Abstractions;
using Jarvis.Domain.Runs;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Application.ToolCalls.AwaitExternalConfirmation;

internal sealed class AwaitExternalConfirmationHandler(
    IRunRepository runs,
    IToolCallRepository calls,
    IEventBus bus,
    TimeProvider time)
    : ICommandHandler<AwaitExternalConfirmationCommand, Guid>
{
    private const string McpChannel = "mcp";

    public async Task<Result<Guid>> Handle(AwaitExternalConfirmationCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        // Idempotency: if an identical action (same user, tool, arguments) is already pending, return it.
        var pending = await calls.ListPendingForUserAsync(command.UserId, cancellationToken);
        var existing = pending.FirstOrDefault(c =>
            c.Tool == command.Tool
            && c.Arguments == command.Arguments
            && c.StatusAt(now) == ToolCallStatus.NeedsConfirmation);

        if (existing is not null)
        {
            return existing.Id;
        }

        // Closed run for an external MCP client
        var run = AssistantRun.Start(
            command.UserId,
            McpChannel,
            command.ClientId,
            null,
            $"External MCP call: {command.Tool}",
            time,
            satellite: false);

        run.Finish("Closed run for external confirmation.", null, null, time);
        runs.Add(run);

        var call = ToolCall.AwaitConfirmation(run.Id, command.UserId, command.Tool, command.Arguments, time);
        calls.Add(call);

        var clientName = string.IsNullOrWhiteSpace(command.ClientId) ? "Aplikacja zewnętrzna" : command.ClientId;
        var toolName = command.Tool;
        var deviceName = string.IsNullOrWhiteSpace(command.DeviceName) ? "komputerze" : command.DeviceName;

        var notification = new NotificationRequested(
            [command.UserId],
            "jarvis",
            "confirmation",
            "Wymagane potwierdzenie",
            $"Aplikacja {clientName} chce wykonać {toolName} na komputerze {deviceName}",
            "/jarvis",
            Tag: $"confirmation-{call.Id}");

        await bus.PublishAsync(notification, cancellationToken);

        return call.Id;
    }
}

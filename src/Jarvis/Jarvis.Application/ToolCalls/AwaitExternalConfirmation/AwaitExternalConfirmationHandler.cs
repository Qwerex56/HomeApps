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

        // Idempotency: if an identical action (same user, tool, arguments) is already pending for this client/channel, return it.
        var pending = await calls.ListPendingForUserAsync(command.UserId, cancellationToken);
        foreach (var p in pending)
        {
            if (p.Tool == command.Tool
                && p.Arguments == command.Arguments
                && p.StatusAt(now) == ToolCallStatus.NeedsConfirmation)
            {
                var associatedRun = await runs.FindAsync(p.RunId, cancellationToken);
                if (associatedRun is not null
                    && associatedRun.Channel == McpChannel
                    && associatedRun.Reference == command.ClientId)
                {
                    return p.Id;
                }
            }
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
        var onDevice = string.IsNullOrWhiteSpace(command.DeviceName) ? "komputerze" : $"komputerze {command.DeviceName}";

        var notification = new NotificationRequested(
            [command.UserId],
            "jarvis",
            "confirmation",
            "Wymagane potwierdzenie",
            $"Aplikacja {clientName} chce wykonać {toolName} na {onDevice}",
            "/jarvis",
            Tag: $"confirmation-{call.Id}");

        await bus.PublishAsync(notification, cancellationToken);

        return call.Id;
    }
}

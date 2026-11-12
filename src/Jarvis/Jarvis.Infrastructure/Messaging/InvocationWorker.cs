using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Assistant;
using Jarvis.Application.Common;
using Jarvis.Application.Runs.Ask;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jarvis.Infrastructure.Messaging;

/// <summary>
/// Answers the channels' invocations one by one, each in its own scope: the run, then <see cref="AssistantResponded"/>
/// through the outbox. An invocation still queued when the service stops is lost; the channel only sees "working".
/// </summary>
internal sealed partial class InvocationWorker(InvocationQueue queue, IServiceScopeFactory scopes, ILogger<InvocationWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var asked in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await AnswerAsync(asked, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogFailed(exception, asked.InvocationId);
            }
        }
    }

    private async Task AnswerAsync(AssistantInvoked asked, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var answer = await sender.Send(
            new AskCommand(
                asked.UserId,
                asked.Text,
                asked.HouseholdId,
                asked.Channel,
                asked.Reference,
                [.. asked.Context.Select(c => new ContextLine(c.AuthorName, c.Text, c.At))]),
            cancellationToken);
        var text = answer.IsSuccess ? answer.Value.Text : Answers.Broken;
        var actions = answer.IsSuccess ? answer.Value.Actions.Select(Cards.Action).ToList() : [];

        await scope.ServiceProvider.GetRequiredService<IEventBus>()
            .PublishAsync(new AssistantResponded(asked.InvocationId, asked.Channel, asked.Reference, text, actions), cancellationToken);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "JARVIS could not answer invocation {InvocationId}")]
    private partial void LogFailed(Exception exception, Guid invocationId);
}

using HomeApps.BuildingBlocks.Application.Abstractions;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Common;
using Jarvis.Application.Gateway;
using Jarvis.Domain.Runs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Jarvis.Application.Runs.Ask;

namespace Jarvis.Application.Runs;

/// <summary>
/// The agent loop around the brain: the switch and the budget first (no model when refused), then the run is saved,
/// so the gateway can log the tool calls into it while the brain works, and finished with the answer and its cost.
/// The actions come from what the gateway logged, whoever the brain is. With a stream id the answer's pieces go to
/// <see cref="IRunFeed"/> as they come (the account's conversation, SSE). Shared by <c>/ask</c>, the conversations and
/// the channels; the caller's command saves the finished run.
/// </summary>
internal sealed partial class Agent(
    IRunRepository runs,
    IToolCallRepository calls,
    IUnitOfWork unitOfWork,
    IDelegation delegation,
    IAssistantBrain brain,
    IOptions<AssistantOptions> options,
    TimeProvider time,
    IRunFeed feed,
    IHouseholdCeilings ceilings,
    IHouseholdDevices householdDevices,
    ILogger<Agent> logger)
{
    public async Task<AnswerView> RunAsync(AskCommand command, Guid? streamId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (await RefusalAsync(command, cancellationToken) is { } refusal)
        {
            var refused = AssistantRun.Refuse(command.UserId, command.Channel, command.Reference, command.HouseholdId, command.Text, refusal, time, command.Satellite);
            runs.Add(refused);
            return new AnswerView(refused.Id, refusal, []);
        }

        var run = AssistantRun.Start(command.UserId, command.Channel, command.Reference, command.HouseholdId, command.Text, time, command.Satellite);
        runs.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (streamId is { } stream)
        {
            feed.Link(stream, run.Id);
        }

        try
        {
            var token = await delegation.TokenForAsync(command.UserId, command.Channel, $"run:{run.Id}", cancellationToken);
            var prompt = Prompts.User(command.Text, command.HouseholdId, command.Context, time.GetLocalNow(), await DevicesAsync(command.HouseholdId, command.UserId, cancellationToken));
            await foreach (var said in brain.RunAsync(new BrainRequest(run.Id, Prompts.SystemFor(command.Channel), prompt, token), cancellationToken))
            {
                if (said is BrainText piece)
                {
                    feed.Publish(run.Id, new RunText(piece.Text));
                }
                else if (said is BrainFinished finished)
                {
                    run.Finish(finished.Text, finished.Model, finished.CostUsd, time);
                }
            }

            run.Fail(Answers.Broken, time);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogRunFailed(run.Id, exception);
            run.Fail(Answers.Broken, time);
        }

        var now = time.GetUtcNow();
        var actions = (await calls.ListForRunAsync(run.Id, cancellationToken))
            .Where(c => !c.ReadOnly)
            .Select(c => ActionView.From(c, now))
            .ToList();
        return new AnswerView(run.Id, run.Response ?? Answers.Broken, actions);
    }

    /// <summary>
    /// "Urządzenia w tym domu" (H-3c): only in a household context, and only while the household lets JARVIS use Devices (H-2); what Devices cannot say leaves it out.
    /// </summary>
    private async Task<string?> DevicesAsync(Guid? householdId, Guid userId, CancellationToken cancellationToken)
    {
        if (householdId is not { } household)
        {
            return null;
        }

        var ceiling = await ceilings.GetAsync(household, cancellationToken);
        if (!ceiling.Enabled || !ceiling.Devices)
        {
            return null;
        }

        return await householdDevices.ListAsync(household, userId, cancellationToken) is { } devices ? DevicesSection.Build(devices) : null;
    }

    private async Task<string?> RefusalAsync(AskCommand command, CancellationToken cancellationToken)
    {
        // H-2: the household's JARVIS speaks on its satellites only while it is on; no model, no tools.
        if (command is { Satellite: true, HouseholdId: { } household })
        {
            var ceiling = await ceilings.GetAsync(household, cancellationToken);
            if (ceiling.SettingsUnavailable)
            {
                return Answers.HouseholdUnavailable;
            }

            if (!ceiling.Enabled)
            {
                return Answers.HouseholdOff;
            }
        }

        var settings = options.Value;
        if (!settings.Enabled || !await brain.IsReadyAsync(cancellationToken))
        {
            return Answers.SwitchedOff;
        }

        if (settings.DailyBudgetUsd is not { } budget)
        {
            return null;
        }

        var local = time.GetLocalNow();
        var midnight = new DateTimeOffset(local.Date, local.Offset);
        return await runs.CostSinceAsync(midnight, cancellationToken) >= budget ? Answers.BudgetSpent : null;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "JARVIS run {RunId} failed.")]
    private partial void LogRunFailed(Guid runId, Exception exception);
}

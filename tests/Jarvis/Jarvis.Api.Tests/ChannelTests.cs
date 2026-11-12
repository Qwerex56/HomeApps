using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Assistant;

namespace Jarvis.Api.Tests;

/// <summary>JARVIS asked from a channel (the chat): he says he is working, then answers to the same reference.</summary>
[Collection(nameof(JarvisCollection))]
public sealed class ChannelTests(JarvisFactory factory)
{
    [Fact]
    public async Task An_invocation_gets_working_then_the_answer_with_its_actions()
    {
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Mleko" })).Text;
        var invocation = Guid.NewGuid();

        await factory.Harness.Bus.Publish(new AssistantInvoked(
            invocation, "chat", "c1/m1", null, Guid.NewGuid(), "usuń mleko",
            [new AssistantContextLine("Ola", "kupiłam mleko", DateTimeOffset.UtcNow)], DateTimeOffset.UtcNow));

        var answer = await AnswerToAsync(invocation);

        Assert.Contains(factory.Published.OfType<AssistantWorking>(), m => m.InvocationId == invocation);
        Assert.Equal("c1/m1", answer.Reference);
        var action = Assert.Single(answer.Actions);
        Assert.NotNull(action.ConfirmationId);
    }

    [Fact]
    public async Task A_click_on_the_chat_card_runs_the_action_and_updates_the_card()
    {
        var person = Guid.NewGuid();
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Chleb" })).Text;
        var invocation = Guid.NewGuid();
        await factory.Harness.Bus.Publish(new AssistantInvoked(
            invocation, "chat", "c1/m2", null, person, "usuń chleb", [], DateTimeOffset.UtcNow));
        var confirmation = Assert.Single((await AnswerToAsync(invocation)).Actions).ConfirmationId!.Value;

        await factory.Harness.Bus.Publish(new AssistantConfirmed(confirmation, person, true, "chat", "c1/m2"));
        var card = await AnswerToAsync(confirmation);

        Assert.Equal("c1/m2", card.Reference);
        Assert.Equal("Deleted \"Chleb\".", Assert.Single(card.Actions).Description);
    }

    private async Task<AssistantResponded> AnswerToAsync(Guid invocation)
    {
        for (var i = 0; i < 100; i++)
        {
            if (factory.Published.OfType<AssistantResponded>().FirstOrDefault(m => m.InvocationId == invocation) is { } answer)
            {
                return answer;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("JARVIS did not answer.");
    }
}

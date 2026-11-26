using HomeApps.BuildingBlocks.Testing;
using HomeApps.Contracts.Assistant;

namespace Jarvis.Api.Tests;

/// <summary>A click on a channel's card that is no longer waiting gets the card's real state back, so its buttons do not hang.</summary>
[Collection(nameof(JarvisCollection))]
public sealed class ConfirmedCardTests(JarvisFactory factory)
{
    [Fact]
    public async Task A_second_click_on_a_decided_card_publishes_the_state_it_really_has()
    {
        var person = Guid.NewGuid();
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Ser" })).Text;
        var invocation = Guid.NewGuid();
        await factory.Harness.Bus.Publish(new AssistantInvoked(
            invocation, "chat", "c1/m7", null, person, "usuń ser", [], DateTimeOffset.UtcNow));
        var confirmation = Assert.Single((await AnswerToAsync(invocation)).Actions).ConfirmationId!.Value;

        await factory.Harness.Bus.Publish(new AssistantConfirmed(confirmation, person, true, "chat", "c1/m7"));
        await factory.Harness.Bus.Publish(new AssistantConfirmed(confirmation, person, true, "chat", "c1/m7"));
        var cards = await CardsAsync(confirmation, 2);

        Assert.All(cards, card => Assert.Equal("c1/m7", card.Reference));
        Assert.All(cards, card => Assert.Equal(confirmation, Assert.Single(card.Actions).ConfirmationId));
        Assert.Equal("Done", Assert.Single(cards[^1].Actions).Status);
    }

    [Fact]
    public async Task A_click_by_someone_else_publishes_nothing_for_the_card()
    {
        var person = Guid.NewGuid();
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Masło" })).Text;
        var invocation = Guid.NewGuid();
        await factory.Harness.Bus.Publish(new AssistantInvoked(
            invocation, "chat", "c1/m8", null, person, "usuń masło", [], DateTimeOffset.UtcNow));
        var confirmation = Assert.Single((await AnswerToAsync(invocation)).Actions).ConfirmationId!.Value;
        var before = factory.Published.OfType<AssistantResponded>().Count(m => m.InvocationId == confirmation);

        await factory.Harness.Bus.Publish(new AssistantConfirmed(confirmation, Guid.NewGuid(), true, "chat", "c1/m8"));
        await Task.Delay(1000);

        Assert.Equal(before, factory.Published.OfType<AssistantResponded>().Count(m => m.InvocationId == confirmation));
    }

    private async Task<IReadOnlyList<AssistantResponded>> CardsAsync(Guid confirmation, int count)
    {
        for (var i = 0; i < 100; i++)
        {
            var cards = factory.Published.OfType<AssistantResponded>().Where(m => m.InvocationId == confirmation).ToList();
            if (cards.Count >= count)
            {
                return cards;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("The card was not published again.");
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

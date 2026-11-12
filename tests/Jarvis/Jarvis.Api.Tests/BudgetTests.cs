namespace Jarvis.Api.Tests;

/// <summary>The daily budget is one for the whole home, so spending it gets its own database.</summary>
public sealed class BudgetTests(JarvisFactory factory) : IClassFixture<JarvisFactory>
{
    [Fact]
    public async Task Over_the_daily_budget_he_refuses_everyone()
    {
        factory.Brain.Cost = 5m;
        await AssistantTests.AskAsync(factory, Guid.NewGuid(), "pierwsze");

        var answer = await AssistantTests.AskAsync(factory, Guid.NewGuid(), "drugie");

        Assert.Equal(Jarvis.Application.Runs.Ask.Answers.BudgetSpent, answer.Text);
    }
}

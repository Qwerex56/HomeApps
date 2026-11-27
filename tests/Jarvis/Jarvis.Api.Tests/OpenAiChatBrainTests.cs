using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Brain;
using Jarvis.Infrastructure.Gateway;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using HomeApps.BuildingBlocks.Testing;

namespace Jarvis.Api.Tests;

/// <summary>The OpenAI-compatible brain against a scripted model and the real gateway (in memory).</summary>
[Collection(nameof(JarvisCollection))]
public sealed class OpenAiChatBrainTests(JarvisFactory factory)
{
    private readonly Guid _person = Guid.NewGuid();

    [Fact]
    public async Task It_streams_the_text_and_calls_the_gateway_tool_the_model_asks_for()
    {
        var model = new FakeModel([FakeModel.Call("call_1", "todo_add_task", """{"title":"Mleko"}"""), FakeModel.Text("Dodałem ", "mleko.")]);

        var said = await RunAsync(model);

        Assert.Equal(["Dodałem ", "mleko."], said.OfType<BrainText>().Select(t => t.Text));
        Assert.Equal("Dodałem mleko.", said.OfType<BrainFinished>().Single().Text);
        var second = model.Requests[1]["messages"]!.AsArray();
        Assert.Equal("tool", second[^1]!["role"]!.GetValue<string>());
        Assert.Equal("call_1", second[^1]!["tool_call_id"]!.GetValue<string>());
        Assert.Equal("Added \"Mleko\".", second[^1]!["content"]!.GetValue<string>());
        Assert.Contains(model.Requests[0]["tools"]!.AsArray(), t => t!["function"]!["name"]!.GetValue<string>() == "todo_add_task");
    }

    [Fact]
    public async Task A_loop_of_tool_calls_stops_at_the_turn_limit()
    {
        var model = new FakeModel { Repeat = () => FakeModel.Call("call", "todo_add_task", """{"title":"Chleb"}""") };

        var said = await RunAsync(model, maxTurns: 3);

        Assert.Equal(3, model.Requests.Count);
        Assert.Single(said.OfType<BrainFinished>());
    }

    [Fact]
    public async Task A_model_that_does_not_answer_in_time_makes_JARVIS_say_he_is_unavailable()
    {
        var model = new FakeModel { Hang = true };

        var said = await RunAsync(model, timeout: TimeSpan.FromMilliseconds(300));

        Assert.Equal("JARVIS jest chwilowo niedostępny", said.OfType<BrainFinished>().Single().Text);
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData("https://models.test/v1", "", false)]
    [InlineData("", "gpt", false)]
    [InlineData("https://models.test/v1", "gpt", true)]
    public void Without_a_URL_and_a_model_it_is_not_ready(string url, string name, bool ready) =>
        Assert.Equal(ready, Brain(new FakeModel(), new OpenAiChatOptions { BaseUrl = url, Model = name }).IsReady);

    [Fact]
    public void The_system_prompt_carries_the_scope_refusal_and_the_voice_one_asks_for_the_screen()
    {
        Assert.Contains("Pomagam tylko w sprawach domu w HomeApps, np. listy, plan, urządzenia. W czym mogę pomóc?", Jarvis.Application.Common.Prompts.SystemFor("app"), StringComparison.Ordinal);
        Assert.Contains("NIE wywołujesz żadnych narzędzi", Jarvis.Application.Common.Prompts.SystemFor("voice"), StringComparison.Ordinal);
        Assert.Contains("na ekranie", Jarvis.Application.Common.Prompts.Spoken, StringComparison.Ordinal);
        Assert.DoesNotContain("\"tak\"", Jarvis.Application.Common.Prompts.Spoken, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_general_question_is_sent_with_the_scope_rule_and_answered_with_the_refusal_without_a_tool_call()
    {
        const string refusal = "Pomagam tylko w sprawach domu w HomeApps, np. listy, plan, urządzenia. W czym mogę pomóc?";
        var model = new FakeModel([FakeModel.Text(refusal)]);

        var said = await RunAsync(model, system: Jarvis.Application.Common.Prompts.SystemFor("app"), prompt: "napisz wypracowanie o Wojnie Trzydziestoletniej");

        Assert.Single(model.Requests);
        var messages = model.Requests[0]["messages"]!.AsArray();
        Assert.Equal("system", messages[0]!["role"]!.GetValue<string>());
        Assert.Contains("ZASADA ZAKRESU", messages[0]!["content"]!.GetValue<string>(), StringComparison.Ordinal);
        Assert.DoesNotContain(messages, m => m!["role"]!.GetValue<string>() == "tool");
        Assert.Equal(refusal, said.OfType<BrainFinished>().Single().Text);
    }

    private async Task<List<BrainEvent>> RunAsync(FakeModel model, int maxTurns = 8, TimeSpan? timeout = null, string system = "system", string prompt = "dodaj mleko")
    {
        var brain = Brain(model, new OpenAiChatOptions
        {
            BaseUrl = "https://models.test/v1",
            Model = "test-model",
            MaxTurns = maxTurns,
            GatewayUrl = "http://localhost/jarvis/mcp",
            Timeout = timeout ?? TimeSpan.FromSeconds(15),
        });
        var request = new BrainRequest(Guid.NewGuid(), system, prompt, TestJwt.CreateToken(_person));
        var said = new List<BrainEvent>();
        await foreach (var e in brain.RunAsync(request, CancellationToken.None))
        {
            said.Add(e);
        }

        return said;
    }

    private OpenAiChatBrain Brain(FakeModel model, OpenAiChatOptions options) => new(
        new StubHttpClients(name => name == OpenAiChatBrain.HttpClientName ? new HttpClient(model) : factory.CreateClient()),
        NullLoggerFactory.Instance,
        Options.Create(options),
        NullLogger<OpenAiChatBrain>.Instance);
}

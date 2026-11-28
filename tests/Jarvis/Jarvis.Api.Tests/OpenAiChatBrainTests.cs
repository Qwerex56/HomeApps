using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Brain;
using Jarvis.Infrastructure.Gateway;
using Microsoft.Extensions.Logging.Abstractions;
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
        Assert.Equal("JARVIS jest chwilowo niedostępny", said.OfType<BrainFinished>().Single().Text);
    }

    [Fact]
    public async Task Keepalives_and_usage_chunks_in_the_stream_are_skipped()
    {
        var model = new FakeModel(["data: \n\n: ping\n\n" + FakeModel.Text("Cześć.").Replace("data: [DONE]", "data: {\"choices\":null,\"usage\":{}}\n\ndata: [DONE]")]);

        var said = await RunAsync(model);

        Assert.Equal("Cześć.", said.OfType<BrainFinished>().Single().Text);
    }

    [Fact]
    public async Task Without_config_the_agent_says_he_is_switched_off_and_the_model_is_not_called()
    {
        var model = new FakeModel();
        var brain = Brain(model, new OpenAiChatOptions());
        factory.Brain.IsReady = await brain.IsReadyAsync(CancellationToken.None);
        try
        {
            var answer = await AssistantTests.AskAsync(factory, _person, "cześć");

            Assert.Equal(Jarvis.Application.Runs.Ask.Answers.SwitchedOff, answer.Text);
            Assert.Empty(model.Requests);
        }
        finally
        {
            factory.Brain.IsReady = true;
        }
    }

    [Fact]
    public async Task A_change_of_the_connection_works_in_the_next_turn_without_a_restart()
    {
        var model = new FakeModel([FakeModel.Text("Pierwsza."), FakeModel.Text("Druga.")]);
        var connection = new FixedConnection(new OpenAiChatOptions { BaseUrl = "https://one.test/v1", Model = "m1", GatewayUrl = "http://localhost/jarvis/mcp" });
        var brain = Brain(model, connection);

        await Drain(brain);
        connection.Options = new OpenAiChatOptions { BaseUrl = "https://two.test/v1", Model = "m2", GatewayUrl = "http://localhost/jarvis/mcp" };
        await Drain(brain);

        Assert.Equal(["m1", "m2"], model.Requests.Select(r => r["model"]!.GetValue<string>()));
    }

    private async Task Drain(OpenAiChatBrain brain)
    {
        var request = new BrainRequest(Guid.NewGuid(), "system", "cześć", TestJwt.CreateToken(_person));
        await foreach (var _ in brain.RunAsync(request, CancellationToken.None))
        {
        }
    }

    [Fact]
    public async Task A_model_that_does_not_answer_in_time_makes_JARVIS_say_he_is_unavailable()
    {
        var model = new FakeModel { Hang = true };

        var said = await RunAsync(model, timeout: TimeSpan.FromMilliseconds(300));

        Assert.Equal("JARVIS jest chwilowo niedostępny", said.OfType<BrainFinished>().Single().Text);
    }

    [Fact]
    public async Task A_model_that_hangs_after_a_tool_ran_does_not_run_the_tool_again()
    {
        using var tools = DeviceTool();
        var model = new FakeModel([FakeModel.Call("call_1", "device_mac_quiet", "{}")]) { HangFromRequest = 2 };

        var said = await RunAsync(model, timeout: TimeSpan.FromMilliseconds(300));

        Assert.Equal("JARVIS jest chwilowo niedostępny", said.OfType<BrainFinished>().Single().Text);
        Assert.Equal(["mac_quiet"], factory.DevicesSource.Calls);
        Assert.Equal(2, model.Requests.Count + 1);
    }

    [Fact]
    public async Task The_model_time_limit_is_per_request_not_for_the_whole_tool_loop()
    {
        using var tools = DeviceTool();
        var model = new FakeModel([
            FakeModel.Call("c1", "device_mac_quiet", "{}"), FakeModel.Call("c2", "device_mac_quiet", """{"a":1}"""), FakeModel.Text("Gotowe.")])
        {
            Delay = TimeSpan.FromMilliseconds(250),
        };

        var said = await RunAsync(model, timeout: TimeSpan.FromMilliseconds(400));

        Assert.Equal("Gotowe.", said.OfType<BrainFinished>().Single().Text);
        Assert.Equal(["mac_quiet", "mac_quiet"], factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task One_model_issued_call_reaches_the_source_exactly_once()
    {
        using var tools = DeviceTool();
        var model = new FakeModel([FakeModel.Call("c1", "device_mac_quiet", "{}"), FakeModel.Text("Zrobione.")]);

        await RunAsync(model);

        Assert.Equal(["mac_quiet"], factory.DevicesSource.Calls);
    }

    [Fact]
    public async Task The_same_action_again_in_one_run_is_not_executed_and_the_model_gets_the_earlier_result()
    {
        using var tools = DeviceTool();
        var model = new FakeModel([
            FakeModel.Call("c1", "device_mac_quiet", """{"level":30,"b":1}"""), FakeModel.Call("c2", "device_mac_quiet", """{"b":1, "level":30}"""), FakeModel.Text("Gotowe.")]);

        var said = await RunAsync(model);

        Assert.Equal("Gotowe.", said.OfType<BrainFinished>().Single().Text);
        Assert.Equal(["mac_quiet"], factory.DevicesSource.Calls);
        var third = model.Requests[2]["messages"]!.AsArray();
        var first = third[^3]!["content"]!.GetValue<string>();
        var again = third[^1]!["content"]!.GetValue<string>();
        Assert.StartsWith(first, again);
        Assert.Contains("Nie wywołuj tego ponownie", again);
    }

    [Fact]
    public async Task The_same_action_in_a_new_run_is_executed()
    {
        using var tools = DeviceTool();

        await RunAsync(new FakeModel([FakeModel.Call("c1", "device_mac_quiet", "{}"), FakeModel.Text("ok")]));
        await RunAsync(new FakeModel([FakeModel.Call("c1", "device_mac_quiet", "{}"), FakeModel.Text("ok")]));

        Assert.Equal(["mac_quiet", "mac_quiet"], factory.DevicesSource.Calls);
    }

    private Reset DeviceTool()
    {
        factory.DevicesSource.Reset();
        factory.DevicesSource.Tools = [FakeToolSet.Computer("mac_quiet", readOnly: false, destructive: false)];
        return new Reset(factory.DevicesSource);
    }

    private sealed class Reset(FakeToolSet set) : IDisposable
    {
        public void Dispose() => set.Reset();
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData("https://models.test/v1", "", false)]
    [InlineData("", "gpt", false)]
    [InlineData("https://models.test/v1", "gpt", true)]
    public async Task Without_a_URL_and_a_model_it_is_not_ready(string url, string name, bool ready) =>
        Assert.Equal(ready, await Brain(new FakeModel(), new OpenAiChatOptions { BaseUrl = url, Model = name }).IsReadyAsync(CancellationToken.None));

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

    private OpenAiChatBrain Brain(FakeModel model, OpenAiChatOptions options) => Brain(model, new FixedConnection(options));

    private OpenAiChatBrain Brain(FakeModel model, FixedConnection options) => new(
        new StubHttpClients(name => name == OpenAiChatBrain.HttpClientName ? new HttpClient(model) : factory.CreateClient()),
        NullLoggerFactory.Instance,
        options,
        NullLogger<OpenAiChatBrain>.Instance);
}

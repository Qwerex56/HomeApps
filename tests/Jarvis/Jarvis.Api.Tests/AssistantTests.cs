using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Common;
using Jarvis.Application.Runs.Ask;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Api.Tests;

/// <summary>JARVIS end to end with a fake brain: what he did, what waits for the person, and the person's decision.</summary>
[Collection(nameof(JarvisCollection))]
public sealed class AssistantTests(JarvisFactory factory) : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly Guid _person = Guid.NewGuid();

    [Fact]
    public async Task Reads_stay_off_the_card_and_safe_changes_are_done_and_shown()
    {
        factory.Brain.Script = async mcp =>
        {
            await mcp.CallTextAsync("todo_my_tasks", new Dictionary<string, object?>());
            return (await mcp.CallTextAsync("todo_add_task", new Dictionary<string, object?> { ["title"] = "Mleko" })).Text;
        };

        var answer = await AskAsync(_person, "dodaj mleko");

        var action = Assert.Single(answer.Actions);
        Assert.Equal("todo_add_task", action.Tool);
        Assert.Equal(ToolCallStatus.Done, action.Status);
        Assert.Equal("Added \"Mleko\".", answer.Text);
        Assert.Contains(factory.Delegation.Calls, c => c.UserId == _person && c.Reference == $"run:{answer.RunId}");
    }

    [Fact]
    public async Task A_destructive_tool_waits_and_runs_only_after_the_person_approves()
    {
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Mleko" })).Text;

        var answer = await AskAsync(_person, "usuń mleko");
        var waiting = Assert.Single(answer.Actions);
        var approved = await DecideAsync(_person, waiting.Id, approve: true);
        var again = await factory.CreateAuthenticatedClient(_person)
            .PostAsJsonAsync($"/jarvis/api/confirmations/{waiting.Id}", new { approve = true });

        Assert.Equal(ToolCallStatus.NeedsConfirmation, waiting.Status);
        Assert.NotNull(waiting.ConfirmBy);
        Assert.Contains("confirmation", answer.Text, StringComparison.Ordinal);
        Assert.Equal(ToolCallStatus.Done, approved.Status);
        Assert.Equal("Deleted \"Mleko\".", approved.Description);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task A_denied_action_is_never_run_and_someone_else_cannot_decide()
    {
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Chleb" })).Text;
        var waiting = Assert.Single((await AskAsync(_person, "usuń chleb")).Actions);

        var stranger = await factory.CreateAuthenticatedClient(Guid.NewGuid())
            .PostAsJsonAsync($"/jarvis/api/confirmations/{waiting.Id}", new { approve = true });
        var denied = await DecideAsync(_person, waiting.Id, approve: false);

        Assert.Equal(HttpStatusCode.NotFound, stranger.StatusCode);
        Assert.Equal(ToolCallStatus.Denied, denied.Status);
    }

    [Fact]
    public async Task Switched_off_he_says_so_and_calls_nothing()
    {
        factory.Brain.IsReady = false;
        factory.Brain.Script = _ => throw new InvalidOperationException("The brain must not run.");

        var answer = await AskAsync(_person, "cześć");

        Assert.Equal(Answers.SwitchedOff, answer.Text);
        Assert.Empty(answer.Actions);
    }

    [Fact]
    public async Task An_assistant_cannot_ask_or_confirm_for_the_person()
    {
        var client = factory.CreateAssistantClient(_person);

        var asked = await client.PostAsJsonAsync("/jarvis/api/ask", new { text = "hej" });
        var confirmed = await client.PostAsJsonAsync($"/jarvis/api/confirmations/{Guid.NewGuid()}", new { approve = true });

        Assert.Equal(HttpStatusCode.Forbidden, asked.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, confirmed.StatusCode);
    }

    public void Dispose()
    {
        factory.Brain.IsReady = true;
    }

    internal static async Task<AnswerView> AskAsync(JarvisFactory factory, Guid person, string text)
    {
        var response = await factory.CreateAuthenticatedClient(person).PostAsJsonAsync("/jarvis/api/ask", new { text });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AnswerView>(Json))!;
    }

    private Task<AnswerView> AskAsync(Guid person, string text) => AskAsync(factory, person, text);

    private async Task<ActionView> DecideAsync(Guid person, Guid action, bool approve)
    {
        var response = await factory.CreateAuthenticatedClient(person)
            .PostAsJsonAsync($"/jarvis/api/confirmations/{action}", new { approve });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ActionView>(Json))!;
    }
}

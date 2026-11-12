using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Common;
using Jarvis.Domain.Conversations;
using Jarvis.Domain.ToolCalls;

namespace Jarvis.Api.Tests;

/// <summary>The person's own conversations in the app, answered live over server-sent events.</summary>
[Collection(nameof(JarvisCollection))]
public sealed class ConversationTests(JarvisFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly Guid _person = Guid.NewGuid();

    [Fact]
    public async Task An_answer_streams_text_and_card_and_stays_in_the_conversation()
    {
        factory.Brain.Script = async mcp =>
            (await mcp.CallTextAsync("todo_delete_task", new Dictionary<string, object?> { ["title"] = "Mleko" })).Text;
        var conversation = await StartAsync(_person);

        var events = await SendAsync(_person, conversation.Id, "usuń mleko z listy");
        var messages = await ReadAsync(_person, conversation.Id);
        var listed = Assert.Single(await factory.CreateAuthenticatedClient(_person)
            .GetFromJsonAsync<List<ConversationView>>("/jarvis/api/conversations", Json) ?? []);

        Assert.Equal(["action", "text", "message"], events.Select(e => e.Event).Distinct());
        var action = events.Single(e => e.Event == "action").Data.Deserialize<ActionView>(Json)!;
        Assert.Equal(ToolCallStatus.NeedsConfirmation, action.Status);
        Assert.Equal([MessageRole.Person, MessageRole.Assistant], messages.Select(m => m.Role));
        Assert.Equal(action.Id, Assert.Single(messages[1].Actions).Id);
        Assert.Equal("usuń mleko z listy", listed.Title);
    }

    [Fact]
    public async Task JARVIS_sees_the_conversation_so_far()
    {
        factory.Brain.Script = _ => Task.FromResult("Zapamiętane.");
        var conversation = await StartAsync(_person);
        await SendAsync(_person, conversation.Id, "mam na imię Ola");

        await SendAsync(_person, conversation.Id, "jak mam na imię?");

        Assert.Contains("mam na imię Ola", factory.Brain.LastPrompt, StringComparison.Ordinal);
        Assert.Contains("Zapamiętane.", factory.Brain.LastPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Someone_elses_conversation_does_not_exist_for_them_and_the_owner_can_delete_it()
    {
        var conversation = await StartAsync(_person);
        var stranger = factory.CreateAuthenticatedClient(Guid.NewGuid());
        var owner = factory.CreateAuthenticatedClient(_person);

        var read = await stranger.GetAsync($"/jarvis/api/conversations/{conversation.Id}/messages");
        var sent = await SendAsync(Guid.NewGuid(), conversation.Id, "hej");
        var deletedByStranger = await stranger.DeleteAsync($"/jarvis/api/conversations/{conversation.Id}");
        var deleted = await owner.DeleteAsync($"/jarvis/api/conversations/{conversation.Id}");
        var gone = await owner.GetAsync($"/jarvis/api/conversations/{conversation.Id}/messages");

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal("jarvis.conversation_not_found", Assert.Single(sent).Data.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NotFound, deletedByStranger.StatusCode);
        Assert.True(deleted.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    private async Task<ConversationView> StartAsync(Guid person)
    {
        var response = await factory.CreateAuthenticatedClient(person).PostAsJsonAsync("/jarvis/api/conversations", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ConversationView>(Json))!;
    }

    private async Task<List<MessageView>> ReadAsync(Guid person, Guid conversation) =>
        (await factory.CreateAuthenticatedClient(person)
            .GetFromJsonAsync<List<MessageView>>($"/jarvis/api/conversations/{conversation}/messages", Json))!;

    private async Task<List<(string Event, JsonElement Data)>> SendAsync(Guid person, Guid conversation, string text)
    {
        var response = await factory.CreateAuthenticatedClient(person)
            .PostAsJsonAsync($"/jarvis/api/conversations/{conversation}/messages", new { text });
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        return body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(block => block.Split('\n'))
            .Select(lines => (
                lines.Single(l => l.StartsWith("event:", StringComparison.Ordinal))["event:".Length..].Trim(),
                Data(string.Join('\n', lines.Where(l => l.StartsWith("data:", StringComparison.Ordinal)).Select(l => l["data:".Length..].TrimStart())))))
            .ToList();
    }

    /// <summary>A text piece is a bare string, everything else JSON.</summary>
    private static JsonElement Data(string data)
    {
        try
        {
            return JsonDocument.Parse(data).RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(data);
        }
    }
}

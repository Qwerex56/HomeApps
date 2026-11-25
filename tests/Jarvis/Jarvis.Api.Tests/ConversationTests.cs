using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Common;
using Jarvis.Domain.Conversations;
using Jarvis.Domain.ToolCalls;
using Jarvis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task A_conversation_can_be_renamed_and_found_by_its_title()
    {
        await StartAsync(_person);
        var other = await StartAsync(_person);
        var owner = factory.CreateAuthenticatedClient(_person);
        var stranger = factory.CreateAuthenticatedClient(Guid.NewGuid());

        var renamed = await owner.PatchAsJsonAsync($"/jarvis/api/conversations/{other.Id}/title", new { title = "Wakacje" });
        var tooLong = await owner.PatchAsJsonAsync($"/jarvis/api/conversations/{other.Id}/title", new { title = new string('a', 81) });
        var foreign = await stranger.PatchAsJsonAsync($"/jarvis/api/conversations/{other.Id}/title", new { title = "Mój" });
        var byTitle = await owner.GetFromJsonAsync<List<ConversationView>>("/jarvis/api/conversations?q=wakac", Json) ?? [];
        var none = await owner.GetFromJsonAsync<List<ConversationView>>("/jarvis/api/conversations?q=szafki", Json) ?? [];
        var tooLongQuery = await owner.GetAsync($"/jarvis/api/conversations?q={new string('a', 101)}");
        var wildcard = await owner.GetFromJsonAsync<List<ConversationView>>("/jarvis/api/conversations?q=%25", Json) ?? [];

        Assert.True(renamed.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(other.Id, Assert.Single(byTitle).Id);
        Assert.Empty(none);
        Assert.Equal(HttpStatusCode.BadRequest, tooLongQuery.StatusCode);
        Assert.Empty(wildcard);
    }

    [Fact]
    public async Task A_phrase_inside_a_message_finds_the_conversation()
    {
        // The SSE answer path is not usable in every environment, so the message is seeded straight into the database.
        var conversation = await StartAsync(_person);
        await StartAsync(_person);
        var owner = factory.CreateAuthenticatedClient(_person);
        await owner.PatchAsJsonAsync($"/jarvis/api/conversations/{conversation.Id}/title", new { title = "Wakacje" });
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JarvisDbContext>();
            var stored = await db.Conversations.SingleAsync(c => c.Id == conversation.Id);
            db.Add(stored.Write("Szafki 60 cm do kuchni", TimeProvider.System));
            await db.SaveChangesAsync();
        }

        var found = await owner.GetFromJsonAsync<List<ConversationView>>("/jarvis/api/v1/conversations?q=SZAFKI", Json) ?? [];
        var none = await owner.GetFromJsonAsync<List<ConversationView>>("/jarvis/api/v1/conversations?q=zmywarka", Json) ?? [];

        Assert.Equal(conversation.Id, Assert.Single(found).Id);
        Assert.Empty(none);
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
            return JsonElement.Parse(data);
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(data);
        }
    }
}

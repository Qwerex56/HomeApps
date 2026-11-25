using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.Contracts.Accounts;
using HomeApps.Contracts.Notifications;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Domain.InApp;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Api.Tests;

/// <summary>2.13: the notification center works without push, respects mutes, replaces tagged entries, keeps 30 days and forgets erased people.</summary>
public sealed class InAppTests(NotificationsApiFactory api) : IClassFixture<NotificationsApiFactory>
{
    [Fact]
    public async Task A_notification_without_a_device_is_listed_and_read_all_clears_the_count()
    {
        var me = Guid.NewGuid();
        await PublishAndWaitAsync(new NotificationRequested([me], "todo", "reminder", "Zadania", "Wynieś śmieci", "/homes"));

        using var client = api.SignedIn(me);
        var list = await client.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app");
        Assert.Equal(1, list.GetProperty("unreadCount").GetInt32());
        Assert.Equal("Wynieś śmieci", Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("body").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/notifications/v1/in-app/read-all", null)).StatusCode);
        var after = await client.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app");
        Assert.Equal(0, after.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task A_single_notification_can_be_marked_read_and_only_its_owner_sees_it()
    {
        var me = Guid.NewGuid();
        await PublishAndWaitAsync(new NotificationRequested([me], "chat", "message", "Dom", "Cześć", "/homes"));
        using var client = api.SignedIn(me);
        var id = (await client.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app")).GetProperty("items")[0].GetProperty("id").GetGuid();

        using var stranger = api.SignedIn(Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/notifications/v1/in-app/{id}/read", null)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/notifications/v1/in-app/{id}/read", null)).StatusCode);
        var unread = await client.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app?unreadOnly=true");
        Assert.Equal(0, unread.GetProperty("unreadCount").GetInt32());
        Assert.Empty(unread.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task A_muted_source_in_a_household_creates_no_entry()
    {
        var household = Guid.NewGuid();
        var me = Guid.NewGuid();
        using (var client = api.SignedIn(me))
        {
            await client.PutAsJsonAsync("/notifications/v1/mutes", new { householdId = household, source = "chat" });
        }

        await PublishAndWaitAsync(new NotificationRequested([me], "chat", "message", "Dom", "Ukryte", "/homes", household));

        using var check = api.SignedIn(me);
        var list = await check.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app");
        Assert.Equal(0, list.GetProperty("unreadCount").GetInt32());
        Assert.Empty(list.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task A_tagged_notification_replaces_the_unread_one_with_the_same_tag()
    {
        var me = Guid.NewGuid();
        var tag = Guid.NewGuid().ToString();
        await PublishAndWaitAsync(new NotificationRequested([me], "chat", "message", "Dom", "Pierwsza", "/homes", Tag: tag));
        await PublishAndWaitAsync(new NotificationRequested([me], "chat", "message", "Dom", "Druga", "/homes", Tag: tag));

        using var client = api.SignedIn(me);
        var list = await client.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app");
        Assert.Equal(1, list.GetProperty("unreadCount").GetInt32());
        Assert.Equal("Druga", Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("body").GetString());
    }

    [Fact]
    public async Task Entries_older_than_thirty_days_are_not_listed()
    {
        var me = Guid.NewGuid();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
            var old = DateTimeOffset.UtcNow.AddDays(-31);
            db.InAppNotifications.Add(InAppNotification.Create(Guid.NewGuid(), me, "todo", "reminder", null, "Stara", "Stara", "/homes", null, old));
            await db.SaveChangesAsync();
        }

        await PublishAndWaitAsync(new NotificationRequested([me], "todo", "reminder", "Zadania", "Nowa", "/homes"));

        using var client = api.SignedIn(me);
        var list = await client.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app");
        Assert.Equal(1, list.GetProperty("unreadCount").GetInt32());
        Assert.Equal("Nowa", Assert.Single(list.GetProperty("items").EnumerateArray()).GetProperty("body").GetString());
    }

    [Fact]
    public async Task An_erased_person_loses_the_whole_notification_center()
    {
        var me = Guid.NewGuid();
        await PublishAndWaitAsync(new NotificationRequested([me], "todo", "reminder", "Zadania", "Zniknie", "/homes"));

        await PublishAndWaitAsync(new UserErased(me));

        using var client = api.SignedIn(me);
        var list = await client.GetFromJsonAsync<JsonElement>("/notifications/v1/in-app");
        Assert.Equal(0, list.GetProperty("unreadCount").GetInt32());
        Assert.Empty(list.GetProperty("items").EnumerateArray());
    }

    private async Task PublishAndWaitAsync<T>(T message)
        where T : class
    {
        var id = Guid.NewGuid();
        await api.Harness.Bus.Publish(message, (PublishContext<T> context) => context.MessageId = id);
        for (var i = 0; i < 100; i++)
        {
            if (await api.Harness.Consumed.Any<T>(c => c.Context.MessageId == id))
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"{typeof(T).Name} was not consumed.");
    }
}

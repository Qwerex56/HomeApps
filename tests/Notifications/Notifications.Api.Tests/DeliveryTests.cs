using System.Net;
using System.Net.Http.Json;
using HomeApps.Contracts.Accounts;
using HomeApps.Contracts.Notifications;
using MassTransit;

namespace Notifications.Api.Tests;

public sealed class DeliveryTests(NotificationsApiFactory api) : IClassFixture<NotificationsApiFactory>
{
    [Fact]
    public async Task A_test_needs_a_device_and_then_reaches_every_device()
    {
        var me = Guid.NewGuid();
        using var client = api.SignedIn(me);
        using var none = await client.PostAsync("/notifications/test", null);
        Assert.Equal(HttpStatusCode.Conflict, none.StatusCode);
        Assert.Equal("notifications.no_devices", await none.CodeAsync());

        var phone = await api.DeviceAsync(me);
        var laptop = await api.DeviceAsync(me);
        using var response = await client.PostAsync("/notifications/test", null);

        Assert.Equal(2, (await response.ReadJsonAsync()).GetProperty("delivered").GetInt32());
        Assert.Equal("HomeApps", Assert.Single(api.Push.SentTo(phone)).Title);
        Assert.Single(api.Push.SentTo(laptop));
    }

    [Fact]
    public async Task A_requested_notification_respects_mutes_and_hidden_previews()
    {
        var household = Guid.NewGuid();
        var anna = Guid.NewGuid();
        var adam = Guid.NewGuid();
        var ewa = Guid.NewGuid();
        var nobody = Guid.NewGuid();
        var annaPhone = await api.DeviceAsync(anna);
        var adamPhone = await api.DeviceAsync(adam);
        var ewaPhone = await api.DeviceAsync(ewa);
        using (var adamClient = api.SignedIn(adam))
        {
            await adamClient.PutAsJsonAsync("/notifications/mutes", new { householdId = household, source = "chat" });
        }

        using (var ewaClient = api.SignedIn(ewa))
        {
            await ewaClient.PutAsJsonAsync("/notifications/settings", new { showPreview = false, quietHours = (object?)null });
        }

        await PublishAndWaitAsync(new NotificationRequested(
            [anna, adam, ewa, nobody], "chat", "message", "Dom", "Tomek: będę o 18", $"/homes/{household}/apps/chat",
            household, "conversation-1", "Nowa wiadomość"));

        var toAnna = Assert.Single(api.Push.SentTo(annaPhone));
        Assert.Equal("Tomek: będę o 18", toAnna.Body);
        Assert.Equal("conversation-1", toAnna.Tag);
        Assert.Equal(household, toAnna.HouseholdId);
        Assert.Empty(api.Push.SentTo(adamPhone));
        Assert.Equal("Nowa wiadomość", Assert.Single(api.Push.SentTo(ewaPhone)).Body);
    }

    [Fact]
    public async Task A_gone_subscription_is_removed()
    {
        var me = Guid.NewGuid();
        var dead = await api.DeviceAsync(me);
        var alive = await api.DeviceAsync(me);
        api.Push.Gone[dead] = true;

        await PublishAndWaitAsync(new NotificationRequested([me], "todo", "reminder", "Zadania", "Wynieś śmieci", "/homes"));

        using var client = api.SignedIn(me);
        var devices = (await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/notifications/settings")).GetProperty("devices");
        Assert.Equal(alive, Assert.Single(devices.EnumerateArray()).GetProperty("endpoint").GetString());
    }

    [Fact]
    public async Task A_request_with_a_full_url_is_dropped()
    {
        var me = Guid.NewGuid();
        var phone = await api.DeviceAsync(me);

        await PublishAndWaitAsync(new NotificationRequested([me], "chat", "message", "Dom", "x", "https://evil.example.com"));

        Assert.Empty(api.Push.SentTo(phone));
    }

    [Fact]
    public async Task Disabled_accounts_lose_their_devices_and_erased_ones_everything()
    {
        var disabled = Guid.NewGuid();
        var erased = Guid.NewGuid();
        await api.DeviceAsync(disabled);
        await api.DeviceAsync(erased);
        using (var client = api.SignedIn(erased))
        {
            await client.PutAsJsonAsync("/notifications/mutes", new { householdId = Guid.NewGuid(), source = (string?)null });
        }

        await PublishAndWaitAsync(new UserDisabled(disabled, BySelf: true));
        await PublishAndWaitAsync(new UserErased(erased));

        using var disabledClient = api.SignedIn(disabled);
        using var erasedClient = api.SignedIn(erased);
        Assert.Empty((await disabledClient.GetFromJsonAsync<System.Text.Json.JsonElement>("/notifications/settings")).GetProperty("devices").EnumerateArray());
        Assert.Empty((await erasedClient.GetFromJsonAsync<System.Text.Json.JsonElement>("/notifications/settings")).GetProperty("mutes").EnumerateArray());
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

            await Task.Delay(100);
        }

        throw new TimeoutException($"{typeof(T).Name} was not consumed.");
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using HomeApps.Contracts.Households;
using MassTransit;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace HomeMix.Api.Tests;

/// <summary>docs/homemix.md, "RealTime": who hears what on <c>/homemix/hub</c> (B2).</summary>
public sealed class RealtimeTests(HomeMixApiFactory api) : IClassFixture<HomeMixApiFactory>
{
    private static readonly byte[] Notifying = AddonZip.Create(AddonZip.Manifest(id: "timer",
        capabilities: """["storage.household", "storage.user", "notify"]"""));

    private static readonly object Consent = new { capabilities = new[] { "storage.household", "storage.user", "notify" }, jarvis = false };

    [Fact]
    public async Task Members_hear_about_household_data_and_only_the_owner_about_their_own()
    {
        var (admin, member, stranger) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(household, Notifying, Consent);
        await using var members = await Listen(member, household);
        await using var strangers = await Listen(stranger, household);

        (await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/data/sessions/a1", new { value = new { n = 1 } })).Dispose();
        (await admins.PutAsJsonAsync($"/homemix/api/installations/{id}/data/notes/mine?scope=user", new { value = "x" })).Dispose();
        (await admins.DeleteAsync($"/homemix/api/installations/{id}/data/sessions/a1")).Dispose();

        var written = await members.NextAsync("data.changed");
        Assert.Equal(id, written.GetProperty("installationId").GetGuid());
        Assert.Equal("household", written.GetProperty("scope").GetString());
        Assert.Equal("sessions", written.GetProperty("collection").GetString());
        Assert.Equal("a1", written.GetProperty("key").GetString());
        Assert.Equal(admin, written.GetProperty("by").GetGuid());
        // The user-scoped write went to the admin alone, so the member's next one is the delete.
        Assert.Equal("sessions", (await members.NextAsync("data.changed")).GetProperty("collection").GetString());
        Assert.False(strangers.Watching);
        Assert.Empty(strangers.Received);
    }

    [Fact]
    public async Task Recipients_with_the_app_open_get_the_notification()
    {
        var (admin, member) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        var id = await admins.InstalledInAsync(household, Notifying, Consent);
        await using var members = await Listen(member, household);

        (await admins.PostAsJsonAsync($"/homemix/api/installations/{id}/notify",
            new { title = "Koniec", body = "Przerwa!", path = "timer", tag = "end" })).Dispose();

        var notification = await members.NextAsync("notification");
        Assert.Equal(id, notification.GetProperty("installationId").GetGuid());
        Assert.Equal("Koniec", notification.GetProperty("title").GetString());
        Assert.Equal("timer", notification.GetProperty("path").GetString());
    }

    [Fact]
    public async Task Installing_removing_and_a_new_role_reach_the_household()
    {
        var (admin, member) = (Guid.NewGuid(), Guid.NewGuid());
        var household = api.Household(admin);
        api.Households.Set(household, member, HouseholdRole.Member);
        using var admins = api.SignedIn(admin);
        await using var members = await Listen(member, household);

        var id = await admins.InstalledInAsync(household, AddonZip.Pomodoro(version: "7.0.0"));
        (await admins.DeleteAsync($"/homemix/api/installations/{id}")).Dispose();
        await api.Services.GetRequiredService<IBus>().Publish(new MemberRoleChanged(household, member, HouseholdRole.Member, HouseholdRole.Admin));

        Assert.Equal("installed", (await members.NextAsync("installation.changed")).GetProperty("change").GetString());
        var removed = await members.NextAsync("installation.changed");
        Assert.Equal("removed", removed.GetProperty("change").GetString());
        Assert.Equal(id, removed.GetProperty("installationId").GetGuid());
        var role = await members.NextAsync("permissions.changed");
        Assert.Equal(household, role.GetProperty("householdId").GetGuid());
        Assert.Equal("Admin", role.GetProperty("role").GetString());
    }

    private async Task<Listener> Listen(Guid userId, Guid household)
    {
        var connection = await api.HubAsync(userId);
        var listener = new Listener(connection);
        listener.Watching = await connection.InvokeAsync<bool>("WatchHousehold", household);
        return listener;
    }

    private sealed class Listener : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly Channel<(string Event, JsonElement Data)> _events = Channel.CreateUnbounded<(string, JsonElement)>();

        public Listener(HubConnection connection)
        {
            _connection = connection;
            connection.On<string, JsonElement>("event", (name, data) =>
            {
                Received.Add(name);
                _events.Writer.TryWrite((name, data));
            });
        }

        public bool Watching { get; set; }

        public List<string> Received { get; } = [];

        /// <summary>The next event with this name; others before it are skipped.</summary>
        public async Task<JsonElement> NextAsync(string name)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (true)
            {
                var (@event, data) = await _events.Reader.ReadAsync(timeout.Token);
                if (@event == name)
                {
                    return data;
                }
            }
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}

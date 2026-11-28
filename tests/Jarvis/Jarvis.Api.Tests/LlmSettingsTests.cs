using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jarvis.Infrastructure.Brain;
using Jarvis.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jarvis.Api.Tests;

/// <summary>The model connection set by an admin in the web app (jarvis-desktop-rework D14).</summary>
[Collection(nameof(JarvisCollection))]
public sealed class LlmSettingsTests(JarvisFactory factory)
{
    private const string Url = "/jarvis/api/v1/admin/llm-settings";
    private const string Secret = "sk-very-secret-key";
    private static readonly object Body = new { baseUrl = "http://127.0.0.1:1/v1", model = "gpt-test", apiKey = Secret, timeoutSeconds = 20 };

    [Fact]
    public async Task A_person_who_is_not_an_admin_gets_403_on_every_endpoint()
    {
        var person = factory.CreateAuthenticatedClient(Guid.NewGuid(), null, "User");

        Assert.Equal(HttpStatusCode.Forbidden, (await person.GetAsync(Url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await person.PutAsJsonAsync(Url, Body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await person.PostAsync(Url + "/test", null)).StatusCode);
    }

    [Fact]
    public async Task The_key_is_stored_protected_and_never_returned()
    {
        await ClearAsync();
        var admin = factory.CreateAuthenticatedClient(Guid.NewGuid(), null, "Admin");

        var saved = await admin.PutAsJsonAsync(Url, Body);
        var text = await (await admin.GetAsync(Url)).Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.DoesNotContain(Secret, await saved.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, text, StringComparison.Ordinal);
        using var json = JsonDocument.Parse(text);
        Assert.True(json.RootElement.GetProperty("apiKeySet").GetBoolean());
        Assert.Equal("gpt-test", json.RootElement.GetProperty("model").GetString());
        using var scope = factory.Services.CreateScope();
        var row = await scope.ServiceProvider.GetRequiredService<JarvisDbContext>().LlmSettings.AsNoTracking().SingleAsync();
        Assert.NotNull(row.ApiKeyProtected);
        Assert.DoesNotContain(Secret, row.ApiKeyProtected, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_save_is_in_force_at_once_and_without_a_key_in_the_body_the_stored_key_stays()
    {
        await ClearAsync();
        var admin = factory.CreateAuthenticatedClient(Guid.NewGuid(), null, "Owner");
        await admin.PutAsJsonAsync(Url, Body);

        await admin.PutAsJsonAsync(Url, new { baseUrl = "http://127.0.0.1:1/v2", model = "other", timeoutSeconds = 30 });

        using var scope = factory.Services.CreateScope();
        var current = await scope.ServiceProvider.GetRequiredService<LlmSettingsStore>().CurrentAsync(CancellationToken.None);
        Assert.Equal("other", current.Model);
        Assert.Equal("http://127.0.0.1:1/v2", current.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(30), current.Timeout);
        Assert.Equal(Secret, current.ApiKey);
    }

    [Fact]
    public async Task Without_saved_settings_the_environment_is_used()
    {
        await ClearAsync();
        using var scope = factory.Services.CreateScope();
        var env = new OpenAiChatOptions { BaseUrl = "https://env.test/v1", Model = "env-model", ApiKey = "env-key" };
        var store = new LlmSettingsStore(
            scope.ServiceProvider.GetRequiredService<JarvisDbContext>(), scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>(), Options.Create(env));

        var current = await store.CurrentAsync(CancellationToken.None);

        Assert.Equal("env-model", current.Model);
        Assert.Equal("env-key", current.ApiKey);
    }

    [Fact]
    public async Task An_invalid_save_is_a_400_and_the_test_reports_an_unreachable_model()
    {
        await ClearAsync();
        var admin = factory.CreateAuthenticatedClient(Guid.NewGuid(), null, "Admin");

        var bad = await admin.PutAsJsonAsync(Url, new { baseUrl = "nope", model = "m", timeoutSeconds = 5 });
        await admin.PutAsJsonAsync(Url, Body);
        var tested = await admin.PostAsync(Url + "/test", null);

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        using var json = JsonDocument.Parse(await tested.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("unreachable", json.RootElement.GetProperty("error").GetString());
    }

    private async Task ClearAsync()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<JarvisDbContext>().LlmSettings.ExecuteDeleteAsync();
    }
}

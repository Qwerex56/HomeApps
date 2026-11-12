using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HomeMix.Api.Tests;

/// <summary>The steps a person takes in the web app: add a package, then install it.</summary>
internal static class Requests
{
    public static async Task<HttpResponseMessage> UploadAsync(this HttpClient client, byte[] zip)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(zip);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(file, "file", "addon.zip");
        return await client.PostAsync("/homemix/api/packages", form);
    }

    /// <summary>Uploads <paramref name="zip"/> and returns its SHA-256.</summary>
    public static async Task<string> PackageAsync(this HttpClient client, byte[] zip)
    {
        using var response = await client.UploadAsync(zip);
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("sha256").GetString()!;
    }

    public static Task<HttpResponseMessage> InstallInAsync(this HttpClient client, Guid household, string sha256, object? consent = null) =>
        client.PostAsJsonAsync($"/homemix/api/households/{household}/installations", new { sha256, consent = consent ?? Household });

    /// <summary>Uploads and installs <paramref name="zip"/> in <paramref name="household"/>; returns the installation id.</summary>
    public static async Task<Guid> InstalledInAsync(this HttpClient client, Guid household, byte[] zip, object? consent = null)
    {
        using var response = await client.InstallInAsync(household, await client.PackageAsync(zip), consent);
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    /// <summary>Consent to what <see cref="AddonZip.Pomodoro"/> asks for by default.</summary>
    public static readonly object Household = new { capabilities = new[] { "storage.household" }, network = Array.Empty<string>(), jarvis = false };

    /// <summary>Consent for a Pomodoro that also fetches from api.open-meteo.com.</summary>
    public static readonly object OpenMeteo = new { capabilities = new[] { "storage.household" }, network = new[] { "https://api.open-meteo.com" }, jarvis = false };

    /// <summary>Consent for a Pomodoro that also sends notifications.</summary>
    public static readonly object Notify = new { capabilities = new[] { "storage.household", "notify" }, jarvis = false };

    /// <summary>Consent for the official Weather of the test instance.</summary>
    public static readonly object Weather = new { capabilities = new[] { "storage.household", "notify" }, network = new[] { "https://api.open-meteo.com" }, jarvis = false };

    /// <summary>Consent for an account add-on with its own data and notifications.</summary>
    public static readonly object UserNotify = new { capabilities = new[] { "storage.user", "notify" }, jarvis = false };

    /// <summary>Consent for <see cref="AddonZip.Notes"/>, JARVIS included.</summary>
    public static readonly object NotesForJarvis = new { capabilities = new[] { "storage.household", "storage.user" }, jarvis = true };

    /// <summary>The same without JARVIS.</summary>
    public static readonly object NotesWithoutJarvis = new { capabilities = new[] { "storage.household", "storage.user" }, jarvis = false };

    /// <summary>Consent for <see cref="AddonZip.Scripts"/>.</summary>
    public static readonly object ScriptsForJarvis = new
    {
        capabilities = new[] { "storage.household", "household.members" },
        network = new[] { "https://api.example.com" },
        jarvis = true,
    };

    /// <summary>Consent for the official Pogoda, JARVIS included.</summary>
    public static readonly object PogodaForJarvis = new
    {
        capabilities = new[] { "storage.household" },
        network = new[] { "https://api.open-meteo.com", "https://geocoding-api.open-meteo.com" },
        jarvis = true,
    };
}

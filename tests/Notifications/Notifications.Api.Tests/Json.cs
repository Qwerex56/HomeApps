using System.Net.Http.Json;
using System.Text.Json;

namespace Notifications.Api.Tests;

internal static class Json
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>())!;

    public static async Task<string?> CodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).TryGetProperty("code", out var code) ? code.GetString() : null;
}

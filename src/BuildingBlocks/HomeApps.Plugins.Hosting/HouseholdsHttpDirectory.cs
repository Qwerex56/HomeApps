using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomeApps.Plugins.Manifest;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// <see cref="IHouseholdDirectory"/> through Households' own endpoints, with the caller's token, so the caller sees exactly
/// what Households shows them. Not cached: names change, and tools ask once per call.
/// </summary>
internal sealed class HouseholdsHttpDirectory(HttpClient http, IHttpContextAccessor accessor, PluginManifest manifest) : IHouseholdDirectory
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<HouseholdName>> MyHouseholdsAsync(CancellationToken cancellationToken) =>
        await GetAsync<HouseholdName>("households", cancellationToken) ?? [];

    public async Task<IReadOnlyList<MemberName>> MemberNamesAsync(Guid householdId, CancellationToken cancellationToken) =>
        await GetAsync<MemberName>($"households/{householdId}/members/names", cancellationToken) ?? [];

    private async Task<List<T>?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var authorization = CallerAuthorization.Read(accessor, manifest)
            ?? throw new InvalidOperationException("The household directory needs the caller's access token; there is no HTTP request with one.");

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);
        using var response = await http.SendAsync(request, cancellationToken);
        return response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<List<T>>(Json, cancellationToken),
            HttpStatusCode.NotFound or HttpStatusCode.Forbidden => null,
            _ => throw new HttpRequestException($"Households answered {(int)response.StatusCode} to {path}.", null, response.StatusCode),
        };
    }
}

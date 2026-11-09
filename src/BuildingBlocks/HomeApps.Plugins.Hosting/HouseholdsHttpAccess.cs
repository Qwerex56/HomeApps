using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Plugins.Manifest;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// <see cref="IHouseholdAccess"/> through <c>GET /households/{id}/members/me</c> on Households, sent with the caller's
/// own access token (from the Authorization header, or from the realtime hub's <c>?access_token=</c>), so Households applies exactly its own rules (archived, guest access ended). Any other answer than
/// 200, 403 or 404 throws: a plugin refuses rather than guesses when Households is down.
/// </summary>
internal sealed class HouseholdsHttpAccess(
    HttpClient http, IHttpContextAccessor accessor, ICurrentUser currentUser, HouseholdAccessCache cache, PluginManifest manifest)
    : IHouseholdAccess
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<HouseholdMembership?> GetMembershipAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequiredUserId;
        if (cache.TryGet(householdId, userId, out var cached))
        {
            return cached;
        }

        var authorization = Authorization();
        if (authorization is null)
        {
            throw new InvalidOperationException("Household access needs the caller's access token; there is no HTTP request with one.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"households/{householdId}/members/me");
        request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);

        using var response = await http.SendAsync(request, cancellationToken);
        HouseholdMembership? membership = response.StatusCode switch
        {
            HttpStatusCode.OK => await response.Content.ReadFromJsonAsync<HouseholdMembership>(Json, cancellationToken),
            HttpStatusCode.NotFound or HttpStatusCode.Forbidden => null,
            _ => throw new HttpRequestException(
                $"Households answered {(int)response.StatusCode} when asked about household {householdId}.", null, response.StatusCode),
        };

        cache.Set(householdId, userId, membership);
        return membership;
    }

    private string? Authorization()
    {
        if (accessor.HttpContext?.Request is not { } request)
        {
            return null;
        }

        var header = request.Headers[HeaderNames.Authorization].ToString();
        if (!string.IsNullOrEmpty(header))
        {
            return header;
        }

        return RealtimeAccessToken.Read(request, manifest) is { } token ? $"Bearer {token}" : null;
    }
}

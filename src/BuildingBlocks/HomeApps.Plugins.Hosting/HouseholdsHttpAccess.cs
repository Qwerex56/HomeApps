using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.Contracts.Households;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HomeApps.Plugins.Hosting;

/// <summary>
/// <see cref="IHouseholdAccess"/> through <c>GET /households/{id}/members/me</c> on Households, sent with the caller's
/// own access token, so Households applies exactly its own rules (archived, guest access ended). Any other answer than
/// 200, 403 or 404 throws: a plugin refuses rather than guesses when Households is down.
/// </summary>
internal sealed class HouseholdsHttpAccess(
    HttpClient http, IHttpContextAccessor accessor, ICurrentUser currentUser, HouseholdAccessCache cache) : IHouseholdAccess
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequiredUserId;
        if (cache.TryGet(householdId, userId, out var cached))
        {
            return cached;
        }

        var authorization = accessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(authorization))
        {
            throw new InvalidOperationException("Household access needs the caller's access token; there is no HTTP request with one.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"households/{householdId}/members/me");
        request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);

        using var response = await http.SendAsync(request, cancellationToken);
        HouseholdRole? role = response.StatusCode switch
        {
            HttpStatusCode.OK => (await response.Content.ReadFromJsonAsync<Membership>(Json, cancellationToken))!.Role,
            HttpStatusCode.NotFound or HttpStatusCode.Forbidden => null,
            _ => throw new HttpRequestException(
                $"Households answered {(int)response.StatusCode} when asked about household {householdId}.", null, response.StatusCode),
        };

        cache.Set(householdId, userId, role);
        return role;
    }

    private sealed record Membership(HouseholdRole Role);
}

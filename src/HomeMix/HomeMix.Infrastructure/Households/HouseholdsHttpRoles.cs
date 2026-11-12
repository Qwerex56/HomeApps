using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HomeMix.Infrastructure.Households;

/// <summary>
/// Asks Households for the caller's role with the caller's own token, as plugins do (docs/plugins.md), so Households
/// applies its own rules (archived, guest access ended). Remembered for the request only: an install checks access
/// twice. Anything but 200, 403 or 404 throws: HomeMix refuses rather than guesses.
/// </summary>
internal sealed class HouseholdsHttpRoles(HttpClient http, IHttpContextAccessor accessor) : IHouseholdRoles
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly Dictionary<Guid, HouseholdRole?> _known = [];

    public async Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken)
    {
        if (_known.TryGetValue(householdId, out var known))
        {
            return known;
        }

        var authorization = accessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(authorization))
        {
            throw new InvalidOperationException("Household roles need the caller's access token; there is no HTTP request with one.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"households/{householdId}/members/me");
        request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);
        using var response = await http.SendAsync(request, cancellationToken);
        HouseholdRole? role = response.StatusCode switch
        {
            HttpStatusCode.OK => (await response.Content.ReadFromJsonAsync<MembershipResponse>(Json, cancellationToken))?.Role,
            HttpStatusCode.NotFound or HttpStatusCode.Forbidden => null,
            _ => throw new HttpRequestException(
                $"Households answered {(int)response.StatusCode} when asked about household {householdId}.", null, response.StatusCode),
        };

        _known[householdId] = role;
        return role;
    }
}

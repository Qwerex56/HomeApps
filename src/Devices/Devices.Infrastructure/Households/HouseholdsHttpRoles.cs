using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Devices.Application.Abstractions;
using HomeApps.Contracts.Households;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Devices.Infrastructure.Households;

/// <summary>
/// Asks Households with the caller's own token (as HomeMix and plugins do), so Households applies its own rules.
/// Remembered for the request only. Anything but 200, 403 or 404 throws: Devices refuses rather than guesses.
/// </summary>
internal sealed class HouseholdsHttpRoles(HttpClient http, IHttpContextAccessor accessor) : IHouseholdRoles
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly Dictionary<Guid, HouseholdRole?> _known = [];
    private IReadOnlyList<Guid>? _mine;

    public async Task<HouseholdRole?> GetRoleAsync(Guid householdId, CancellationToken cancellationToken)
    {
        if (_known.TryGetValue(householdId, out var known))
        {
            return known;
        }

        using var request = Request($"households/{householdId}/members/me");
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

    public async Task<IReadOnlyList<Guid>> GetMyHouseholdIdsAsync(CancellationToken cancellationToken)
    {
        if (_mine is not null)
        {
            return _mine;
        }

        using var request = Request("households/");
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var mine = await response.Content.ReadFromJsonAsync<List<MyHouseholdResponse>>(Json, cancellationToken) ?? [];
        foreach (var household in mine)
        {
            _known[household.Id] = household.MyRole;
        }

        return _mine = [.. mine.Select(h => h.Id)];
    }

    private HttpRequestMessage Request(string path)
    {
        var authorization = accessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(authorization))
        {
            throw new InvalidOperationException("Households is asked with the caller's access token; there is no HTTP request with one.");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization);
        return request;
    }
}

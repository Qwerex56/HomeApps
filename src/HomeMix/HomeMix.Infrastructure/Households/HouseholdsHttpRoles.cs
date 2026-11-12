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
/// Asks Households for the caller's role (and a household's members) with the caller's own token, as plugins do (docs/plugins.md), so Households
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

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        using var request = Request($"households/{householdId}/members/names");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        var names = await response.Content.ReadFromJsonAsync<List<MemberNameResponse>>(Json, cancellationToken);
        return [.. (names ?? []).Select(n => n.UserId)];
    }

    public async Task<IReadOnlyList<HouseholdMember>> GetMembersAsync(Guid householdId, CancellationToken cancellationToken)
    {
        using var request = Request($"households/{householdId}/members/");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        var members = await response.Content.ReadFromJsonAsync<List<MemberResponse>>(Json, cancellationToken);
        return [.. (members ?? [])
            .Where(m => m.IsActive && m.Role is not null)
            .Select(m => new HouseholdMember(m.UserId, m.DisplayName, m.AvatarUrl, m.Role!.Value))];
    }

    public async Task<IReadOnlyList<MyHousehold>> GetMyHouseholdsAsync(CancellationToken cancellationToken)
    {
        using var request = Request("households/");
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var mine = await response.Content.ReadFromJsonAsync<List<MyHouseholdResponse>>(Json, cancellationToken);
        var households = (mine ?? []).Select(h => new MyHousehold(h.Id, h.Name, h.MyRole)).ToList();
        foreach (var household in households)
        {
            _known[household.Id] = household.Role;
        }

        return households;
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

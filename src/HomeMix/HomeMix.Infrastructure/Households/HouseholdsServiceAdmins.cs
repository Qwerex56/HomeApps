using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.Contracts.Households;
using HomeMix.Application.Abstractions;

namespace HomeMix.Infrastructure.Households;

/// <summary>
/// Households' <c>/internal/households/{id}/members</c> with HomeMix's own service client (<c>HomeMix:ClientId</c>/<c>ClientSecret</c>;
/// Households lists it under <c>Households:Internal:HomeMix</c>), for who to tell when a trigger turns itself off.
/// </summary>
internal sealed class HouseholdsServiceAdmins(HttpClient http) : IHouseholdAdmins
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<IReadOnlyList<Guid>> GetAdminIdsAsync(Guid householdId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync($"internal/v1/households/{householdId}/members", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        var people = await response.Content.ReadFromJsonAsync<List<PersonResponse>>(Json, cancellationToken) ?? [];
        return [.. people.Where(p => p.Role >= HouseholdRole.Admin).Select(p => p.UserId)];
    }
}

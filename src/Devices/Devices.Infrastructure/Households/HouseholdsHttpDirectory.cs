using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Devices.Application.Abstractions;
using Devices.Domain.Devices;
using HomeApps.Contracts.Households;
using Microsoft.Extensions.Configuration;

namespace Devices.Infrastructure.Households;

/// <summary>
/// Households' <c>/internal/households/{id}/members</c> and <c>/assistant</c> with Devices' own service client
/// (<c>Devices:ClientId</c>, <c>Devices:ClientSecret</c>, as <c>X-Client-Id</c>/<c>X-Client-Secret</c>); Households lists
/// this client under <c>Households:Internal:Devices</c>. Anything but success throws, so a job tries again later.
/// </summary>
internal sealed class HouseholdsHttpDirectory(HttpClient http, IConfiguration configuration) : IHouseholdDirectory
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<IReadOnlyList<Guid>> GetAdminIdsAsync(Guid householdId, CancellationToken cancellationToken) =>
        [.. (await PeopleAsync(householdId, cancellationToken)).Where(p => p.Role >= HouseholdRole.Admin).Select(p => p.UserId)];

    public async Task<IReadOnlyList<Guid>> GetMemberIdsAsync(Guid householdId, CancellationToken cancellationToken) =>
        [.. (await PeopleAsync(householdId, cancellationToken)).Select(p => p.UserId)];

    private async Task<List<PersonResponse>> PeopleAsync(Guid householdId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync($"internal/households/{householdId}/members", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PersonResponse>>(Json, cancellationToken) ?? [];
    }

    public async Task<EventRetention> GetEventRetentionAsync(Guid householdId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync($"internal/households/{householdId}/assistant", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return EventRetention.Default;
        }

        response.EnsureSuccessStatusCode();
        var assistant = await response.Content.ReadFromJsonAsync<AssistantResponse>(Json, cancellationToken)
            ?? throw new HttpRequestException("Households sent no assistant settings.");
        return new EventRetention(assistant.EventDays, assistant.LocationEventDays);
    }

    private Task<HttpResponseMessage> SendAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Client-Id", configuration["Devices:ClientId"] ?? "devices-service");
        request.Headers.Add("X-Client-Secret", configuration["Devices:ClientSecret"] ?? string.Empty);
        return http.SendAsync(request, cancellationToken);
    }
}

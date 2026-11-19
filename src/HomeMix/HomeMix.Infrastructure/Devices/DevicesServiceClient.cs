using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentResults;
using HomeMix.Application.Abstractions;
using HomeMix.Application;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Triggers;
using Microsoft.Extensions.Options;

namespace HomeMix.Infrastructure.Devices;

/// <summary>
/// Devices as HomeMix itself (service to service, <c>X-Client-Id</c>/<c>X-Client-Secret</c> from <c>HomeMix:ClientId</c>/<c>ClientSecret</c>
/// as Devices lists them under <c>Devices:Internal:HomeMix</c>), for the triggers of add-ons, which run when nobody is logged in.
/// Devices sees the call as the add-on (installation, name, the installer) and checks the installer is an admin there.
/// </summary>
internal sealed class DevicesServiceClient(HttpClient http, IOptions<HomeMixOptions> options) : IAddonDevices
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<HouseholdDevice>> ListAsync(Guid householdId, Guid asUser, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.Value.ClientSecret))
        {
            throw new InvalidOperationException(TriggerErrors.NoServiceClient.Message);
        }

        using var response = await http.GetAsync($"internal/households/{householdId}/devices?user={asUser}", cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return [];
        }

        DevicesHttp.EnsureSuccess(response);
        var devices = await response.Content.ReadFromJsonAsync<List<DescriptionResponse>>(Json, cancellationToken);
        return [.. (devices ?? []).Select(d => d.ToDevice())];
    }

    public async Task<Result> SetAsync(
        Installation installation, string addonName, Guid deviceId, string control, JsonElement value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(installation);
        if (string.IsNullOrEmpty(options.Value.ClientSecret))
        {
            return Result.Fail(TriggerErrors.NoServiceClient);
        }

        var body = new
        {
            householdId = installation.TargetId,
            control,
            value,
            installerUserId = installation.InstalledBy,
            installationId = installation.Id,
            addonName,
        };
        using var response = await http.PostAsJsonAsync($"internal/devices/{deviceId}/set", body, Json, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.Unauthorized)
        {
            DevicesHttp.EnsureSuccess(response);
        }

        return Result.Fail(await DevicesHttp.ErrorAsync(response, cancellationToken));
    }
}

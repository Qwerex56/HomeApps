using System.Net.Http.Json;
using Jarvis.Application.Abstractions;
using Jarvis.Application.Gateway;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Gateway;

/// <summary>
/// Devices' <c>GET /internal/households/{id}/devices</c> with JARVIS's service secret, kept 30 seconds per household (a turn reads it once;
/// a device going offline shows within half a minute). A failure (the client gives up after 3 seconds) is kept 5 seconds, so a down Devices
/// is not asked on every turn; the caller then leaves the devices out.
/// </summary>
internal sealed partial class HouseholdDevices(HttpClient http, IOptions<GatewayOptions> options, IMemoryCache cache, ILogger<HouseholdDevices> logger) : IHouseholdDevices
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FailureLifetime = TimeSpan.FromSeconds(5);

    public async Task<IReadOnlyList<HouseholdDevice>?> ListAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var key = (nameof(HouseholdDevices), householdId);
        if (cache.TryGetValue(key, out Cached? known) && known is not null)
        {
            return known.Devices;
        }

        try
        {
            var settings = options.Value;
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.DevicesUrl.TrimEnd('/')}/internal/households/{householdId}/devices");
            request.Headers.Add("X-Client-Id", settings.ClientId);
            request.Headers.Add("X-Client-Secret", settings.ClientSecret);
            using var response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var views = await response.Content.ReadFromJsonAsync<List<View>>(cancellationToken) ?? [];
            return Keep(key, [.. views.Select(v => new HouseholdDevice(v.Id, v.Name, v.Purpose, v.Online, v.Silent, v.LowBattery, v.Battery, v.Jarvis, v.AdminNotes))], Lifetime);
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogUnreadable(householdId, exception);
            return Keep(key, null, FailureLifetime);
        }
    }

    private IReadOnlyList<HouseholdDevice>? Keep((string, Guid) key, IReadOnlyList<HouseholdDevice>? devices, TimeSpan lifetime)
    {
        cache.Set(key, new Cached(devices), lifetime);
        return devices;
    }

    private sealed record Cached(IReadOnlyList<HouseholdDevice>? Devices);

    private sealed record View(Guid Id, string Name, string Purpose, bool Online, bool Silent, bool LowBattery, double? Battery, string? Jarvis, string? AdminNotes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The devices of household {HouseholdId} did not load; JARVIS goes without the device section for now.")]
    private partial void LogUnreadable(Guid householdId, Exception exception);
}

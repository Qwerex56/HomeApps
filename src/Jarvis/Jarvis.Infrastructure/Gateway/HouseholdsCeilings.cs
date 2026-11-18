using System.Net.Http.Json;
using Jarvis.Application.Gateway;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Gateway;

/// <summary>
/// Households' <c>GET /internal/households/{id}/assistant</c> with JARVIS's service secret, kept 45 seconds per household
/// (an admin's change reaches the satellite within a minute). A failure (the client gives up after 3 seconds) makes the household
/// <see cref="HouseholdCeiling.Unavailable"/> (as closed as <see cref="HouseholdCeiling.Closed"/>) and is kept only 5 seconds, so a
/// down Households is not asked on every call.
/// </summary>
internal sealed partial class HouseholdsCeilings(
    HttpClient http, IOptions<GatewayOptions> options, IMemoryCache cache, ILogger<HouseholdsCeilings> logger, TimeSpan? failureLifetime = null) : IHouseholdCeilings
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(45);
    private readonly TimeSpan _failureLifetime = failureLifetime ?? TimeSpan.FromSeconds(5);

    public async Task<HouseholdCeiling> GetAsync(Guid householdId, CancellationToken cancellationToken)
    {
        var key = (nameof(HouseholdsCeilings), householdId);
        if (cache.TryGetValue(key, out HouseholdCeiling? known) && known is not null)
        {
            return known;
        }

        try
        {
            var settings = options.Value;
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.HouseholdsUrl.TrimEnd('/')}/internal/households/{householdId}/assistant");
            request.Headers.Add("X-Client-Id", settings.ClientId);
            request.Headers.Add("X-Client-Secret", settings.ClientSecret);
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return Keep(key, HouseholdCeiling.Closed);
            }

            response.EnsureSuccessStatusCode();
            var view = await response.Content.ReadFromJsonAsync<View>(cancellationToken);
            return view is null
                ? Keep(key, HouseholdCeiling.Unavailable, _failureLifetime)
                : Keep(key, HouseholdCeiling.From(view.Enabled, view.AccessLevel, view.ToolSources));
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogUnreadable(householdId, exception);
            return Keep(key, HouseholdCeiling.Unavailable, _failureLifetime);
        }
    }

    private HouseholdCeiling Keep((string, Guid) key, HouseholdCeiling ceiling, TimeSpan? lifetime = null)
    {
        cache.Set(key, ceiling, lifetime ?? Lifetime);
        return ceiling;
    }

    private sealed record View(bool Enabled, string? AccessLevel, string? ToolSources);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The assistant settings of household {HouseholdId} did not load; its tools are limited to the core ones for now.")]
    private partial void LogUnreadable(Guid householdId, Exception exception);
}

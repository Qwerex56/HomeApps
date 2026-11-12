using System.Net.Http.Json;
using Jarvis.Application.Gateway;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Gateway;

/// <summary>
/// Auth's <c>POST /internal/delegation</c> with the service client's secret. A token is kept per person and client
/// until shortly before it expires, so one conversation does not ask Auth for every tool call.
/// </summary>
internal sealed class AuthDelegation(HttpClient http, IOptions<GatewayOptions> options, IMemoryCache cache, TimeProvider time) : IDelegation
{
    private static readonly TimeSpan Margin = TimeSpan.FromSeconds(30);

    public async Task<string> TokenForAsync(Guid userId, string channel, string reference, CancellationToken cancellationToken)
    {
        var key = (nameof(AuthDelegation), userId, channel, reference);
        if (cache.TryGetValue(key, out string? cached) && cached is not null)
        {
            return cached;
        }

        var settings = options.Value;
        using var response = await http.PostAsJsonAsync("internal/delegation", new
        {
            settings.ClientId,
            settings.ClientSecret,
            userId,
            channel,
            reference,
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<DelegatedToken>(cancellationToken)
            ?? throw new InvalidOperationException("Auth answered the delegation without a token.");

        var keep = token.ExpiresAt - time.GetUtcNow() - Margin;
        if (keep > TimeSpan.Zero)
        {
            cache.Set(key, token.AccessToken, keep);
        }

        return token.AccessToken;
    }
}

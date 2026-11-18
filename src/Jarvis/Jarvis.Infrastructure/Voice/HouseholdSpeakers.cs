using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jarvis.Application.Common;
using Jarvis.Application.Gateway;
using Jarvis.Application.Voice;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Voice;

/// <summary>
/// The household's people from Households (<c>GET /households/{id}/members</c> with the satellite's delegated token),
/// narrowed by the spoken name, then Auth's <c>POST /internal/devices/speaker</c> with JARVIS's service secret.
/// </summary>
internal sealed class HouseholdSpeakers(HttpClient http, IDelegation delegation, IOptions<GatewayOptions> options) : ISpeakers
{
    public async Task<SpeakerMatch> IdentifyAsync(Guid deviceId, Guid householdId, string? name, string pin, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var token = await delegation.TokenForAsync(deviceId, Prompts.VoiceChannel, $"device/{deviceId}", cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{settings.HouseholdsUrl.TrimEnd('/')}/households/{householdId}/members");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var listed = await http.SendAsync(request, cancellationToken);
        listed.EnsureSuccessStatusCode();
        var people = (await listed.Content.ReadFromJsonAsync<List<Member>>(cancellationToken) ?? [])
            .Where(m => m is { IsActive: true, IsDevice: false })
            .ToList();
        var named = people.Where(m => PinIntro.SameName(name, m.Nickname) || PinIntro.SameName(name, m.DisplayName) || PinIntro.SameName(name, m.Name)).ToList();
        var candidates = named.Count > 0 ? named : people;
        if (candidates.Count == 0)
        {
            return new SpeakerMatch(SpeakerOutcome.Unknown);
        }

        using var asked = await http.PostAsJsonAsync($"{settings.AuthUrl.TrimEnd('/')}/internal/devices/speaker", new
        {
            settings.ClientId,
            settings.ClientSecret,
            deviceId,
            candidates = candidates.Select(m => m.UserId).Take(100),
            pin,
        }, cancellationToken);
        switch (asked.StatusCode)
        {
            case HttpStatusCode.Conflict:
                return new SpeakerMatch(SpeakerOutcome.Ambiguous);
            case HttpStatusCode.Locked:
                return new SpeakerMatch(SpeakerOutcome.Locked);
        }

        asked.EnsureSuccessStatusCode();
        var speaker = await asked.Content.ReadFromJsonAsync<Speaker>(cancellationToken);
        return candidates.FirstOrDefault(m => m.UserId == speaker?.UserId) is { } person
            ? new SpeakerMatch(SpeakerOutcome.Known, person.UserId, person.Nickname ?? person.DisplayName)
            : new SpeakerMatch(SpeakerOutcome.Unknown);
    }

    private sealed record Member(Guid UserId, string Name, string? Nickname, string DisplayName, bool IsActive, bool IsDevice);

    private sealed record Speaker(Guid? UserId);
}

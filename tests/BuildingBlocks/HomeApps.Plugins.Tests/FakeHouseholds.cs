using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HomeApps.Contracts.Households;

namespace HomeApps.Plugins.Tests;

/// <summary>Stands in for Households' <c>GET /households/{id}/members/me</c>; reads the user from a test header.</summary>
public sealed class FakeHouseholds : HttpMessageHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    /// <summary>When everyone set with <see cref="SetRole"/> joined.</summary>
    public static readonly DateTimeOffset JoinedAt = new(2026, 11, 1, 18, 0, 0, TimeSpan.Zero);

    private readonly ConcurrentDictionary<(Guid, Guid), HouseholdRole> _roles = new();
    private int _calls;

    public int Calls => _calls;

    public string? LastAuthorization { get; private set; }

    /// <summary>Names for <c>GET /households</c> and <c>GET /households/{id}/members/names</c>.</summary>
    public ConcurrentDictionary<Guid, string> HouseholdNames { get; } = new();

    public ConcurrentDictionary<Guid, string> PeopleNames { get; } = new();

    public void SetRole(Guid householdId, Guid userId, HouseholdRole? role)
    {
        if (role is { } value)
        {
            _roles[(householdId, userId)] = value;
        }
        else
        {
            _roles.TryRemove((householdId, userId), out _);
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        LastAuthorization = request.Headers.Authorization?.ToString();

        var segments = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
        var userId = Guid.Parse(TestUser.Of(LastAuthorization));
        if (segments.Length == 1)
        {
            var mine = _roles.Keys.Where(k => k.Item2 == userId)
                .Select(k => new { id = k.Item1, name = HouseholdNames.GetValueOrDefault(k.Item1, "?") });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(mine, options: Json) });
        }

        var householdId = Guid.Parse(segments[1]);
        if (segments[^1] == "names")
        {
            var people = _roles.Keys.Where(k => k.Item1 == householdId)
                .Select(k => new { userId = k.Item2, name = PeopleNames.GetValueOrDefault(k.Item2, "?"), avatarUrl = (string?)null });
            return Task.FromResult(_roles.ContainsKey((householdId, userId))
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(people, options: Json) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return Task.FromResult(_roles.TryGetValue((householdId, userId), out var role)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { householdId, role, nickname = (string?)null, joinedAt = JoinedAt, accessUntil = (DateTimeOffset?)null }, options: Json) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

internal static class TestUser
{
    /// <summary>The <c>sub</c> of a test token, read without validating it (Households would validate it).</summary>
    public static string Of(string? authorization)
    {
        var payload = authorization!["Bearer ".Length..].Split('.')[1];
        var padded = payload.Replace('-', '+').Replace('_', '/').PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(padded));
        return json.RootElement.GetProperty("sub").GetString()!;
    }
}

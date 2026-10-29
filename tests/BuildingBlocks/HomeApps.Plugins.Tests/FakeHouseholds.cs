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
    private readonly ConcurrentDictionary<(Guid, Guid), HouseholdRole> _roles = new();
    private int _calls;

    public int Calls => _calls;

    public string? LastAuthorization { get; private set; }

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
        var householdId = Guid.Parse(segments[1]);
        var userId = Guid.Parse(TestUser.Of(LastAuthorization));

        return Task.FromResult(_roles.TryGetValue((householdId, userId), out var role)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { householdId, role }, options: Json) }
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

using System.Net;

namespace Accounts.Api.Tests;

/// <summary>Release 3.0: the new path and the deprecated one answer alike; the old one says it is going away.</summary>
public sealed class VersioningTests(AccountsApiFactory api) : IClassFixture<AccountsApiFactory>
{
    [Fact]
    public async Task The_legacy_path_answers_with_deprecation_headers_and_the_v1_path_without()
    {
        using var client = api.SignedIn(await api.RegisterAsync());

        using var current = await client.GetAsync(new Uri("/users/v1/me", UriKind.Relative));
        using var legacy = await client.GetAsync(new Uri("/users/me", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        Assert.False(current.Headers.Contains("Deprecation"));
        Assert.Equal("true", Assert.Single(legacy.Headers.GetValues("Deprecation")));
        Assert.Equal("Sat, 01 Jan 2028 00:00:00 GMT", Assert.Single(legacy.Headers.GetValues("Sunset")));
    }
}

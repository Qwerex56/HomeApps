namespace Auth.Api.Tests;

/// <summary>Device tokens limited to 3 a minute, browsers' refresh to 3: shows that the two buckets are separate (H-4c item 2).</summary>
public sealed class DeviceTokenRateLimitFactory : AuthApiFactory
{
    protected override int RefreshPerMinute => 3;

    protected override int DeviceTokenPerMinute => 3;
}

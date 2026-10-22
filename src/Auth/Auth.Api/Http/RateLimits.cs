using System.Globalization;
using System.Threading.RateLimiting;
using HomeApps.BuildingBlocks.Api.Results;
using Microsoft.Extensions.Options;

namespace Auth.Api.Http;

/// <summary>Section <c>Auth:RateLimits</c>. In memory (Q17): one instance, a restart starts from zero.</summary>
internal sealed class RateLimitOptions
{
    public const string SectionName = "Auth:RateLimits";

    /// <summary>Anonymous attempts per IP and minute: sign-in, sign-up, refresh, links from emails.</summary>
    public int PerMinute { get; set; } = 10;

    /// <summary>Requests that send an email, per IP and hour.</summary>
    public int MailPerHour { get; set; } = 10;

    /// <summary>Password reset emails per address and hour.</summary>
    public int MailPerAddressPerHour { get; set; } = 3;
}

internal static class RateLimits
{
    public const string Anonymous = "auth-anon";
    public const string Mail = "auth-mail";

    public static IServiceCollection AddAuthRateLimits(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>().BindConfiguration(RateLimitOptions.SectionName);
        services.AddSingleton<PerAddressLimiter>();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                http.Response.Headers.RetryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture)
                    : "60";
                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = http,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Too many attempts. Wait a moment and try again.",
                        Extensions = { [ResultHttpMapper.CodeExtension] = "rate_limited" },
                    },
                });
            };

            limiter.AddPolicy(Anonymous, http => RateLimitPartition.GetSlidingWindowLimiter(ClientIp(http), _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = Limits(http).PerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                }));

            limiter.AddPolicy(Mail, http => RateLimitPartition.GetFixedWindowLimiter(ClientIp(http), _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = Limits(http).MailPerHour,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));
        });

        return services;
    }

    /// <summary>After <c>UseForwardedHeaders</c>, so behind Traefik this is the caller, not the proxy.</summary>
    private static string ClientIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static RateLimitOptions Limits(HttpContext http) =>
        http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
}

/// <summary>A-08: at most a few reset emails per address, whoever asks. Over the limit the answer is still 202 (R9).</summary>
internal sealed class PerAddressLimiter(IOptions<RateLimitOptions> options) : IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter = PartitionedRateLimiter.Create<string, string>(address =>
        RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.Value.MailPerAddressPerHour,
            Window = TimeSpan.FromHours(1),
            QueueLimit = 0,
        }));

    public bool TryAcquire(string address)
    {
        using var lease = _limiter.AttemptAcquire(address.Trim().ToUpperInvariant());
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}

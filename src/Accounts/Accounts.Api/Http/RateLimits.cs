using System.Globalization;
using System.Threading.RateLimiting;
using HomeApps.BuildingBlocks.Api.Auth;
using HomeApps.BuildingBlocks.Api.Results;
using Microsoft.Extensions.Options;

namespace Accounts.Api.Http;

/// <summary>Section <c>Accounts:RateLimits</c>. In memory like Auth (Q17): one instance, a restart starts from zero.</summary>
internal sealed class RateLimitOptions
{
    public const string SectionName = "Accounts:RateLimits";

    /// <summary>Changes one person may make in <see cref="Window"/>, on any account (Q6, Q9).</summary>
    public int Changes { get; set; } = 10;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>
/// Q6: a flood of edits (a stolen token, a script) must not fill an account's history. Every write counts against the
/// caller's one budget, also an admin's edits of other accounts (Q9); reads are free.
/// </summary>
internal static class RateLimits
{
    public const string Changes = "accounts-changes";

    public static IServiceCollection AddAccountsRateLimits(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>().BindConfiguration(RateLimitOptions.SectionName);

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
                        Detail = "Too many changes in a short time. Wait a moment and try again.",
                        Extensions = { [ResultHttpMapper.CodeExtension] = "rate_limited" },
                    },
                });
            };

            limiter.AddPolicy(Changes, http =>
            {
                var limits = http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                return RateLimitPartition.GetSlidingWindowLimiter(Caller(http), _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = limits.Changes,
                    Window = limits.Window,
                    SegmentsPerWindow = 10,
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }

    /// <summary>The signed-in user (or ticket holder); the limiter runs after authorization, so <c>sub</c> is known.</summary>
    private static string Caller(HttpContext http) =>
        http.User.FindFirst(ClaimNames.Subject)?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

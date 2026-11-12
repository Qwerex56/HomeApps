using System.Globalization;
using System.Threading.RateLimiting;
using HomeMix.Application;
using Microsoft.Extensions.Options;

namespace HomeMix.Api.Http;

/// <summary>
/// <c>HomeMix:NotificationsPerHour</c> per installation, so one add-on cannot flood a household. In memory: one
/// instance, a restart starts from zero.
/// </summary>
internal static class NotifyLimit
{
    public const string Policy = "homemix-notify";

    public static IServiceCollection AddNotifyLimit(this IServiceCollection services) =>
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture)
                        : "60";
                return ValueTask.CompletedTask;
            };
            limiter.AddPolicy(Policy, http =>
            {
                var permits = http.RequestServices.GetRequiredService<IOptions<HomeMixOptions>>().Value.NotificationsPerHour;
                return RateLimitPartition.GetSlidingWindowLimiter(
                    http.Request.RouteValues["id"]?.ToString() ?? string.Empty,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permits,
                        Window = TimeSpan.FromHours(1),
                        SegmentsPerWindow = 12,
                        QueueLimit = 0,
                    });
            });
        });
}

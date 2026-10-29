using System.Globalization;
using System.Threading.RateLimiting;
using HomeApps.BuildingBlocks.Api.Auth;
using HomeApps.BuildingBlocks.Api.Results;
using Microsoft.Extensions.Options;

namespace Households.Api.Http;

/// <summary>Section <c>Households:RateLimits</c>. In memory like Auth and Accounts: one instance, a restart starts from zero.</summary>
internal sealed class RateLimitOptions
{
    public const string SectionName = "Households:RateLimits";

    /// <summary>Q8: invite links opened or accepted (I4, I5) per <see cref="InvitesWindow"/>, against guessing codes.</summary>
    public int Invites { get; set; } = 10;

    public TimeSpan InvitesWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Q8: every other write per <see cref="ChangesWindow"/>, on any household.</summary>
    public int Changes { get; set; } = 30;

    public TimeSpan ChangesWindow { get; set; } = TimeSpan.FromMinutes(10);
}

/// <summary>Q8: two budgets per signed-in user; reads other than invite links are free.</summary>
internal static class RateLimits
{
    public const string Invites = "households-invites";
    public const string Changes = "households-changes";

    public static IServiceCollection AddHouseholdsRateLimits(this IServiceCollection services)
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
                        Detail = "Too many requests in a short time. Wait a moment and try again.",
                        Extensions = { [ResultHttpMapper.CodeExtension] = "rate_limited" },
                    },
                });
            };

            limiter.AddPolicy(Invites, http =>
            {
                var limits = http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                return SlidingWindow(http, limits.Invites, limits.InvitesWindow);
            });

            limiter.AddPolicy(Changes, http =>
            {
                var limits = http.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                return SlidingWindow(http, limits.Changes, limits.ChangesWindow);
            });
        });

        return services;
    }

    private static RateLimitPartition<string> SlidingWindow(HttpContext http, int permits, TimeSpan window) =>
        RateLimitPartition.GetSlidingWindowLimiter(Caller(http), _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = permits,
            Window = window,
            SegmentsPerWindow = 10,
            QueueLimit = 0,
        });

    /// <summary>The signed-in user; the limiter runs after authorization, so <c>sub</c> is known.</summary>
    private static string Caller(HttpContext http) =>
        http.User.FindFirst(ClaimNames.Subject)?.Value ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

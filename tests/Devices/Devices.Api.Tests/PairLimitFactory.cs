using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Devices.Api.Tests;

/// <summary>The Devices Api behind a proxy (loopback, X-Forwarded-For honoured) with only 3 pairing attempts a minute per address.</summary>
public sealed class PairLimitFactory : DevicesApiFactory
{
    public const int PairPerMinute = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.ConfigureWebHost(builder);
        builder.UseSetting("Devices:RateLimits:PairPerMinute", PairPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.ConfigureServices(services => services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, FixedRemoteIpFilter>());
    }
}

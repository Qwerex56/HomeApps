using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeApps.BuildingBlocks.Infrastructure.Options;

public static class OptionsExtensions
{
    /// <summary>
    /// Binds <typeparamref name="TOptions"/> to a configuration section, validates data annotations
    /// and fails at startup (not at first use) when the configuration is wrong.
    /// </summary>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class
    {
        return services
            .AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}

using Microsoft.Extensions.Configuration;
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

    /// <summary>Reads and validates options immediately, for values needed while registering services.</summary>
    public static TOptions GetValidatedOptions<TOptions>(this IConfiguration configuration, string sectionName)
        where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(sectionName).Get<TOptions>() ?? new TOptions();
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(options);
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(options, context, validateAllProperties: true);
        return options;
    }
}

using System.Reflection;
using FluentValidation;
using HomeApps.BuildingBlocks.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HomeApps.BuildingBlocks.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR (handlers from <paramref name="applicationAssembly"/>), the logging → validation → transaction
    /// pipeline and every FluentValidation validator in the assembly.
    /// </summary>
    /// <param name="licenseKey">The free MediatR community licence key, read from configuration <c>MediatR:LicenseKey</c>.</param>
    public static IServiceCollection AddHomeAppsApplication(this IServiceCollection services, Assembly applicationAssembly, string? licenseKey = null)
    {
        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = licenseKey;
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        return services;
    }
}

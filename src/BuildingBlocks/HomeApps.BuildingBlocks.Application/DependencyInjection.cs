using System.Globalization;
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
    /// pipeline and every FluentValidation validator in the assembly. Validation messages are in Polish, the only language
    /// of the web app (frontend plan Q5); the front shows them under the field named in the error.
    /// </summary>
    public static IServiceCollection AddHomeAppsApplication(this IServiceCollection services, Assembly applicationAssembly)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(applicationAssembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("pl");

        return services;
    }
}

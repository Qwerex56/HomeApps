using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.MassTransit;

public static class MassTransitExtension {
    public static void AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration) {
        services.AddMassTransit(config => {
            var rabbitMqConfig = configuration.GetSection("RabbitMqOptions");
            
            config.UsingRabbitMq((ctx, cfg) => {
                cfg.Host(rabbitMqConfig["Host"], rabbitMqConfig["VirtualHost"], h => {
                    h.Username(rabbitMqConfig["Username"]);
                    h.Password(rabbitMqConfig["Password"]);
                });
                
                cfg.ConfigureEndpoints(ctx);
            });
        });
    }
}
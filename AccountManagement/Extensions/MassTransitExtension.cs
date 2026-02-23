using AccountManagement.Options;
using MassTransit;
using Microsoft.Extensions.Options;

namespace AccountManagement.Extensions;

public static class MassTransitExtension {
    public static void RegisterMassTransit(this IServiceCollection services, IOptions<RabbitMqOptions> options) {
        services.AddMassTransit(config => {
            var rabbitMqOptions = options.Value;

            // Register consumers


            // Use RabbitMq
            config.UsingRabbitMq((ctx, cfg) => {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, h => {
                    h.Username(rabbitMqOptions.UserName);
                    h.Password(rabbitMqOptions.Password);
                });
            });
        });
    }
}
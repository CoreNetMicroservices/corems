using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers MassTransit with the appropriate transport (RabbitMQ, in-memory, etc.)
    /// based on configuration. Services only interact via IPublishEndpoint and IConsumer.
    ///
    /// Usage in Program.cs:
    ///   builder.Services.AddCoreMsMessaging(builder.Configuration, cfg => {
    ///       cfg.AddConsumer&lt;MyConsumer&gt;();
    ///   });
    /// </summary>
    public static IServiceCollection AddCoreMsMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddMassTransit(x =>
        {
            configureConsumers?.Invoke(x);

            var connectionString = configuration.GetConnectionString("rabbitmq");

            if (!string.IsNullOrEmpty(connectionString))
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(connectionString));
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                // Fallback: in-memory transport for development without RabbitMQ
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}

using CoreMs.Common.Middleware;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreMs.Common.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Registers MassTransit with the appropriate transport (RabbitMQ, in-memory, etc.)
    /// based on configuration. Automatically propagates correlation IDs from HTTP context
    /// to published messages. Services interact via IPublishEndpoint and IConsumer.
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

                    // Propagate correlation ID from HTTP context to message headers
                    cfg.ConfigurePublish(pipeline =>
                        pipeline.UseExecute(publishContext => SetCorrelationId(publishContext, context)));
                });
            }
            else
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);

                    cfg.ConfigurePublish(pipeline =>
                        pipeline.UseExecute(publishContext => SetCorrelationId(publishContext, context)));
                });
            }
        });

        return services;
    }

    private static void SetCorrelationId(PublishContext publishContext, IServiceProvider sp)
    {
        if (publishContext.CorrelationId.HasValue)
            return;

        var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
        var correlationId = CorrelationIdMiddleware.GetCorrelationId(httpContextAccessor?.HttpContext);

        if (correlationId is not null && Guid.TryParse(correlationId, out var guid))
            publishContext.CorrelationId = guid;
        else
            publishContext.CorrelationId = Guid.NewGuid();
    }
}

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CoreMs.ServiceDefaults;

public static class CoreMsHost
{
    /// <summary>Health check tag for readiness probes (dependencies must be reachable).</summary>
    public const string ReadyTag = "ready";

    /// <summary>Health check tag for liveness probes (process is up).</summary>
    public const string LiveTag = "live";

    /// <summary>
    /// Registers Aspire service defaults: OpenTelemetry (traces, metrics, runtime instrumentation),
    /// per-dependency health checks, and service discovery.
    /// Call this first in Program.cs before AddCoreMsApp().
    /// </summary>
    public static IHostApplicationBuilder AddCoreMsHost(this IHostApplicationBuilder builder)
    {
        var serviceName = builder.Environment.ApplicationName;
        var serviceVersion = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "1.0.0";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: Environment.MachineName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .UseOtlpExporter();

        builder.AddCoreMsHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());

        return builder;
    }

    /// <summary>
    /// Registers a liveness "self" check plus per-dependency readiness checks that are
    /// auto-detected from configured connection strings. A service only gets a Postgres check
    /// if "ConnectionStrings:corems" is present, so the same defaults work across all services
    /// regardless of their dependencies.
    ///
    /// RabbitMQ health is covered by MassTransit's own "masstransit-bus" check (tagged "ready"),
    /// which AddCoreMsMessaging registers automatically only in services that use messaging —
    /// so it appears in /ready and /health for those services and nowhere else, with no duplicate
    /// broker connection.
    /// </summary>
    private static void AddCoreMsHealthChecks(this IHostApplicationBuilder builder)
    {
        var checks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LiveTag]);

        var postgres = builder.Configuration.GetConnectionString("corems");
        if (!string.IsNullOrWhiteSpace(postgres))
            checks.AddNpgSql(postgres, name: "postgres", tags: [ReadyTag]);
    }

    /// <summary>
    /// Maps the health check endpoints:
    ///   /alive  — liveness  (process is up; "self" check only)
    ///   /ready  — readiness (all dependency checks tagged "ready")
    ///   /health — full report (every registered check)
    /// These are plain HTTP endpoints consumed by container/orchestrator probes — no Aspire required.
    /// </summary>
    public static WebApplication MapCoreMsEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true
        });

        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(LiveTag)
        });

        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains(ReadyTag)
        });

        return app;
    }
}

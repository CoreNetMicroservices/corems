using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CoreMs.Common.Observability;

/// <summary>
/// Output format for application logs.
/// </summary>
public enum CoreMsLogFormat
{
    /// <summary>Human-readable text, one line per event. Best for local development.</summary>
    Console,

    /// <summary>Newline-delimited compact JSON (CLEF). Best for Splunk / log aggregators.</summary>
    Json
}

/// <summary>
/// Strongly-typed logging configuration bound from the <c>CoreMsLogging</c> config section.
/// </summary>
public sealed class CoreMsLoggingOptions
{
    public const string SectionName = "CoreMsLogging";

    /// <summary>
    /// Output format. When null, the format is chosen automatically:
    /// <see cref="CoreMsLogFormat.Console"/> in Development, <see cref="CoreMsLogFormat.Json"/> otherwise.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Minimum level threshold. Accepts friendly aliases (INFO, WARN, ERROR) and the
    /// standard Serilog levels (Verbose, Debug, Information, Warning, Error, Fatal).
    /// Defaults to Information when null or unrecognized.
    /// </summary>
    public string? MinimumLevel { get; set; }
}

/// <summary>
/// Central Serilog configuration for all CoreMS services. Applies uniform enrichment and
/// switches between human-readable console output and compact JSON based on config.
/// </summary>
public static class CoreMsLogging
{
    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId:l} {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Builds the Serilog logger configuration for a service. Reads the <c>CoreMsLogging</c>
    /// section, then still applies <c>ReadFrom.Configuration</c> so advanced tuning via the
    /// standard <c>Serilog</c> section remains available.
    /// </summary>
    public static void Configure(
        LoggerConfiguration cfg,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName)
    {
        var options = configuration.GetSection(CoreMsLoggingOptions.SectionName)
            .Get<CoreMsLoggingOptions>() ?? new CoreMsLoggingOptions();

        var format = ResolveFormat(options.Format, environment);
        var minimumLevel = ResolveLevel(options.MinimumLevel);

        cfg
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new ActivityEnricher())
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .ReadFrom.Configuration(configuration);

        if (format == CoreMsLogFormat.Json)
            cfg.WriteTo.Console(new CompactJsonFormatter());
        else
            cfg.WriteTo.Console(outputTemplate: ConsoleTemplate);
    }

    /// <summary>
    /// Resolves the effective format: explicit config wins, otherwise auto
    /// (Console in Development, Json elsewhere).
    /// </summary>
    public static CoreMsLogFormat ResolveFormat(string? configured, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(configured)
            && Enum.TryParse<CoreMsLogFormat>(configured, ignoreCase: true, out var parsed))
            return parsed;

        return environment.IsDevelopment() ? CoreMsLogFormat.Console : CoreMsLogFormat.Json;
    }

    /// <summary>
    /// Resolves the minimum level, accepting friendly aliases (INFO/WARN/ERROR) and standard
    /// Serilog level names. Falls back to Information.
    /// </summary>
    public static LogEventLevel ResolveLevel(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
            return LogEventLevel.Information;

        return configured.Trim().ToUpperInvariant() switch
        {
            "VERBOSE" or "TRACE" => LogEventLevel.Verbose,
            "DEBUG" => LogEventLevel.Debug,
            "INFO" or "INFORMATION" => LogEventLevel.Information,
            "WARN" or "WARNING" => LogEventLevel.Warning,
            "ERROR" => LogEventLevel.Error,
            "FATAL" or "CRITICAL" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}

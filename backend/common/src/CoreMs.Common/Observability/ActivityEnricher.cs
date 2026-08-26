using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace CoreMs.Common.Observability;

/// <summary>
/// Enriches every log event with the current distributed trace context (TraceId, SpanId) read
/// from <see cref="Activity.Current"/>. OpenTelemetry's ASP.NET Core and HttpClient
/// instrumentation populate this, so log lines can be correlated with traces in any backend
/// (Aspire dashboard, Splunk, Grafana, App Insights) by TraceId.
///
/// No-op when there is no active Activity (e.g. background work outside a request).
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        AddProperty(logEvent, propertyFactory, "TraceId", activity.TraceId.ToString());
        AddProperty(logEvent, propertyFactory, "SpanId", activity.SpanId.ToString());
    }

    private static void AddProperty(
        LogEvent logEvent, ILogEventPropertyFactory factory, string name, string value)
    {
        logEvent.AddPropertyIfAbsent(factory.CreateProperty(name, value));
    }
}

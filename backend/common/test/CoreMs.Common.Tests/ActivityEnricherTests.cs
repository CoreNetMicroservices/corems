using System.Diagnostics;
using CoreMs.Common.Observability;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace CoreMs.Common.Tests;

public class ActivityEnricherTests
{
    private sealed class TestPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            => new(name, new ScalarValue(value));
    }

    private static LogEvent NewLogEvent() => new(
        DateTimeOffset.UtcNow,
        LogEventLevel.Information,
        exception: null,
        new MessageTemplate("test", Array.Empty<MessageTemplateToken>()),
        Array.Empty<LogEventProperty>());

    [Fact]
    public void Enrich_WithActiveActivity_AddsTraceIdAndSpanId()
    {
        using var source = new ActivitySource("CoreMs.Tests");
        using var listener = AllDataListener("CoreMs.Tests");

        using var activity = source.StartActivity("op");
        activity.Should().NotBeNull();

        var logEvent = NewLogEvent();
        new ActivityEnricher().Enrich(logEvent, new TestPropertyFactory());

        logEvent.Properties.Should().ContainKey("TraceId");
        logEvent.Properties.Should().ContainKey("SpanId");

        var traceId = ((ScalarValue)logEvent.Properties["TraceId"]).Value as string;
        var spanId = ((ScalarValue)logEvent.Properties["SpanId"]).Value as string;

        traceId.Should().Be(activity!.TraceId.ToString());
        spanId.Should().Be(activity.SpanId.ToString());
    }

    [Fact]
    public void Enrich_WithNoActivity_AddsNothing()
    {
        // Ensure no ambient activity leaks from other tests.
        Activity.Current = null;

        var logEvent = NewLogEvent();
        new ActivityEnricher().Enrich(logEvent, new TestPropertyFactory());

        logEvent.Properties.Should().NotContainKey("TraceId");
        logEvent.Properties.Should().NotContainKey("SpanId");
    }

    private static ActivityListener AllDataListener(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}

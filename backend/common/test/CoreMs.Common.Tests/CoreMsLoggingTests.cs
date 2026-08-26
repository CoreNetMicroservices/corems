using CoreMs.Common.Observability;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Serilog.Events;
using Xunit;

namespace CoreMs.Common.Tests;

public class CoreMsLoggingTests
{
    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "TestApp";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static readonly IHostEnvironment Development = new FakeEnvironment(Environments.Development);
    private static readonly IHostEnvironment Production = new FakeEnvironment(Environments.Production);

    // ---- ResolveFormat --------------------------------------------------

    [Fact]
    public void ResolveFormat_NullInDevelopment_DefaultsToConsole()
        => CoreMsLogging.ResolveFormat(null, Development).Should().Be(CoreMsLogFormat.Console);

    [Fact]
    public void ResolveFormat_NullInProduction_DefaultsToJson()
        => CoreMsLogging.ResolveFormat(null, Production).Should().Be(CoreMsLogFormat.Json);

    [Fact]
    public void ResolveFormat_NullInTesting_DefaultsToConsole()
        => CoreMsLogging.ResolveFormat(null, new FakeEnvironment("Testing")).Should().Be(CoreMsLogFormat.Console);

    [Fact]
    public void ResolveFormat_EmptyInProduction_DefaultsToJson()
        => CoreMsLogging.ResolveFormat("   ", Production).Should().Be(CoreMsLogFormat.Json);

    [Theory]
    [InlineData("Console", CoreMsLogFormat.Console)]
    [InlineData("console", CoreMsLogFormat.Console)]
    [InlineData("JSON", CoreMsLogFormat.Json)]
    [InlineData("json", CoreMsLogFormat.Json)]
    public void ResolveFormat_ExplicitValue_OverridesEnvironment(string configured, CoreMsLogFormat expected)
    {
        // Explicit config wins regardless of environment.
        CoreMsLogging.ResolveFormat(configured, Development).Should().Be(expected);
        CoreMsLogging.ResolveFormat(configured, Production).Should().Be(expected);
    }

    [Fact]
    public void ResolveFormat_UnrecognizedValue_FallsBackToEnvironmentDefault()
    {
        CoreMsLogging.ResolveFormat("banana", Development).Should().Be(CoreMsLogFormat.Console);
        CoreMsLogging.ResolveFormat("banana", Production).Should().Be(CoreMsLogFormat.Json);
    }

    // ---- ResolveLevel ---------------------------------------------------

    [Theory]
    [InlineData("INFO", LogEventLevel.Information)]
    [InlineData("Information", LogEventLevel.Information)]
    [InlineData("info", LogEventLevel.Information)]
    [InlineData("WARN", LogEventLevel.Warning)]
    [InlineData("Warning", LogEventLevel.Warning)]
    [InlineData("warn", LogEventLevel.Warning)]
    [InlineData("ERROR", LogEventLevel.Error)]
    [InlineData("error", LogEventLevel.Error)]
    [InlineData("DEBUG", LogEventLevel.Debug)]
    [InlineData("VERBOSE", LogEventLevel.Verbose)]
    [InlineData("TRACE", LogEventLevel.Verbose)]
    [InlineData("FATAL", LogEventLevel.Fatal)]
    [InlineData("CRITICAL", LogEventLevel.Fatal)]
    public void ResolveLevel_KnownAliases_MapToExpectedLevel(string configured, LogEventLevel expected)
        => CoreMsLogging.ResolveLevel(configured).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nonsense")]
    public void ResolveLevel_NullEmptyOrUnknown_DefaultsToInformation(string? configured)
        => CoreMsLogging.ResolveLevel(configured).Should().Be(LogEventLevel.Information);

    [Fact]
    public void ResolveLevel_TrimsAndIgnoresCase()
        => CoreMsLogging.ResolveLevel("  WaRn  ").Should().Be(LogEventLevel.Warning);
}

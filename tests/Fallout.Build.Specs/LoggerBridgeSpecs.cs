using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Execution;
using Fallout.Common.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers the <see cref="ILogger" /> seam over Serilog added for #428. Serilog stays the provider;
/// the bridge only has to be faithful — same severities, same message templates, same exceptions,
/// and no second level authority of its own.
/// </summary>
/// <remarks>
/// Exercising the seam means writing through the ambient <see cref="Log.Logger" />, which is
/// process-wide: spec classes outside this collection run in parallel and log as they go, so every
/// message here carries <see cref="Marker" /> and the collected events are filtered down to it.
/// Without that, a stray warning from another class lands in the sink and fails an assertion.
/// </remarks>
[Collection(ProcessGlobalStateCollection.Name)]
public class LoggerBridgeSpecs
{
    /// <summary>Distinguishes this class's log records from those of concurrently running specs.</summary>
    private const string Marker = "loggerbridgespec";

    [Theory]
    [InlineData(MsLogLevel.Trace, LogEventLevel.Verbose)]
    [InlineData(MsLogLevel.Debug, LogEventLevel.Debug)]
    [InlineData(MsLogLevel.Information, LogEventLevel.Information)]
    [InlineData(MsLogLevel.Warning, LogEventLevel.Warning)]
    [InlineData(MsLogLevel.Error, LogEventLevel.Error)]
    [InlineData(MsLogLevel.Critical, LogEventLevel.Fatal)]
    public void Each_logger_level_maps_to_its_serilog_level(MsLogLevel level, LogEventLevel expected)
    {
        var events = Capture(logger => logger.Log(level, Marker + " a line"));

        events.Should().ContainSingle().Which.Level.Should().Be(expected);
    }

    [Fact]
    public void The_bridge_does_not_filter_below_information()
    {
        // Regression guard for the AddFalloutLogging registration. Wiring the seam through
        // services.AddLogging(...) would install Microsoft.Extensions.Logging's own filter pipeline,
        // whose default minimum is Information — trace and debug records would vanish before Serilog
        // ever saw them, and the level switch would no longer be the only thing that decides.
        using var pipeline = PreserveAmbientPipeline();
        using var services = new ServiceCollection().AddFalloutLogging().BuildServiceProvider();

        var events = CaptureAmbient(() =>
        {
            var logger = services.GetRequiredService<ILogger<LoggerBridgeSpecs>>();
            logger.LogTrace(Marker + " trace line");
            logger.LogDebug(Marker + " debug line");
        });

        events.Select(x => x.Level).Should().Equal(LogEventLevel.Verbose, LogEventLevel.Debug);
    }

    [Fact]
    public void The_level_switch_still_gates_the_bridge()
    {
        var original = FalloutBuild.Verbosity;
        try
        {
            FalloutBuild.Verbosity = Verbosity.Minimal;

            var events = Capture(
                logger =>
                {
                    logger.LogInformation(Marker + " below the gate");
                    logger.LogWarning(Marker + " above the gate");
                },
                configuration => configuration.MinimumLevel.ControlledBy(Logging.LevelSwitch));

            events.Should().ContainSingle().Which.Level.Should().Be(LogEventLevel.Warning);
        }
        finally
        {
            FalloutBuild.Verbosity = original;
        }
    }

    [Fact]
    public void Message_templates_survive_the_bridge()
    {
        const string Template = Marker + " restored {PackageCount} packages";

        var events = Capture(logger => logger.LogInformation(Template, 12));

        var logEvent = events.Should().ContainSingle().Subject;
        // The template must stay a template — a pre-rendered string would defeat the structured
        // sinks (the compact-JSON interceptor formatter, the file sinks) downstream.
        logEvent.MessageTemplate.Text.Should().Be(Template);
        logEvent.Properties.Should().ContainKey("PackageCount")
            .WhoseValue.Should().BeOfType<ScalarValue>()
            .Which.Value.Should().Be(12);
    }

    [Fact]
    public void Exceptions_reach_the_log_event()
    {
        var exception = new InvalidOperationException("boom");

        var events = Capture(logger => logger.LogError(exception, Marker + " the target failed"));

        events.Should().ContainSingle().Which.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void The_factory_is_not_pinned_to_one_pipeline()
    {
        // Log.Logger is reassigned during a run — Configure installs it late, and
        // Host.WriteErrorsAndWarnings swaps it again for the end-of-build summary. The factory
        // itself must stay unbound so a logger it creates afterwards lands in the current pipeline.
        var factory = Logging.CreateSerilogLoggerFactory();
        var first = new CollectingSink();
        var second = new CollectingSink();

        using (PreserveAmbientPipeline())
        {
            Log.Logger = CreateLogger(first);
            factory.CreateLogger(Logging.DefaultCategoryName).LogWarning(Marker + " before the swap");

            Log.Logger = CreateLogger(second);
            factory.CreateLogger(Logging.DefaultCategoryName).LogWarning(Marker + " after the swap");
        }

        first.Marked.Should().ContainSingle();
        second.Marked.Should().ContainSingle();
    }

    [Fact]
    public void The_facade_logger_tracks_the_current_pipeline()
    {
        // Binding happens once per logger, not once per write, because the category is attached as
        // SourceContext at construction. Logging.Logger is therefore deliberately uncached — that is
        // what keeps the façade pointed at whichever pipeline is installed right now.
        var sink = new CollectingSink();

        using (PreserveAmbientPipeline())
        {
            var before = Logging.Logger;

            Log.Logger = CreateLogger(sink);
            Logging.Logger.LogWarning(Marker + " after the swap");

            Logging.Logger.Should().NotBeSameAs(before);
        }

        sink.Marked.Should().ContainSingle();
    }

    [Fact]
    public void The_facade_works_without_a_composition_root()
    {
        // The CLI commands call Logging.Configure() directly, with no container in sight, so the
        // façade has to fall back to the ambient pipeline rather than throw.
        var events = CaptureAmbient(() => Logging.Logger.LogInformation(Marker + " no container here"));

        events.Should().ContainSingle().Which.Level.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void The_container_resolves_the_logging_abstractions()
    {
        using var pipeline = PreserveAmbientPipeline();
        using var services = new ServiceCollection().AddFalloutLogging().BuildServiceProvider();

        services.GetRequiredService<ILoggerFactory>().Should().NotBeNull();
        services.GetRequiredService<ILogger<LoggerBridgeSpecs>>().Should().NotBeNull();
        services.GetRequiredService<ILogger>().Should().NotBeNull();
    }

    [Fact]
    public void Using_a_logger_factory_restores_the_previous_one()
    {
        var previous = Logging.Factory;
        var replacement = new StubLoggerFactory();

        using (Logging.UseLoggerFactory(replacement))
        {
            Logging.Factory.Should().BeSameAs(replacement);
        }

        Logging.Factory.Should().BeSameAs(previous);
    }

    [Fact]
    public void Using_a_null_logger_factory_is_rejected()
    {
        var act = () => Logging.UseLoggerFactory(factory: null);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Writes through a bridged logger against a pipeline that collects this class's records, and
    /// returns them. <paramref name="configure" /> overrides the minimum-level rule.
    /// </summary>
    private static LogEvent[] Capture(
        Action<ILogger> write,
        Func<LoggerConfiguration, LoggerConfiguration> configure = null)
    {
        return CaptureAmbient(
            () => write.Invoke(Logging.CreateSerilogLoggerFactory().CreateLogger(Logging.DefaultCategoryName)),
            configure);
    }

    /// <summary>Runs <paramref name="write" /> against a collecting <see cref="Log.Logger" />.</summary>
    private static LogEvent[] CaptureAmbient(
        Action write,
        Func<LoggerConfiguration, LoggerConfiguration> configure = null)
    {
        var sink = new CollectingSink();
        using (PreserveAmbientPipeline())
        {
            Log.Logger = CreateLogger(sink, configure);
            write.Invoke();
        }

        return sink.Marked.ToArray();
    }

    /// <summary>Restores the ambient Serilog pipeline when the returned bracket is disposed.</summary>
    private static IDisposable PreserveAmbientPipeline()
    {
        var original = Log.Logger;
        return DelegateDisposable.CreateBracket(cleanup: () => Log.Logger = original);
    }

    private static Serilog.Core.Logger CreateLogger(
        ILogEventSink sink,
        Func<LoggerConfiguration, LoggerConfiguration> configure = null)
    {
        var configuration = new LoggerConfiguration();
        configuration = configure?.Invoke(configuration) ?? configuration.MinimumLevel.Verbose();
        return configuration.WriteTo.Sink(sink).CreateLogger();
    }

    private class CollectingSink : ILogEventSink
    {
        private readonly List<LogEvent> events = new();

        /// <summary>The records this class wrote, with concurrent specs' traffic filtered out.</summary>
        public IReadOnlyList<LogEvent> Marked
        {
            get
            {
                lock (events)
                    return events.Where(x => x.MessageTemplate.Text.Contains(Marker)).ToList();
            }
        }

        public void Emit(LogEvent logEvent)
        {
            lock (events)
                events.Add(logEvent);
        }
    }

    private class StubLoggerFactory : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName) => throw new NotSupportedException();

        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

        public void Dispose()
        {
        }
    }
}

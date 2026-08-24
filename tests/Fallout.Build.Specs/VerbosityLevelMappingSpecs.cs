using System;
using System.Collections.Generic;
using System.Linq;
using Fallout.Common.Execution;
using FluentAssertions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers the verbosity ladder and the conversions between <see cref="Verbosity" />,
/// <see cref="LogLevel" /> and Serilog's <see cref="LogEventLevel" />. See #556.
///
/// Before this, there was no setting that showed information and above: Normal mapped to Serilog
/// Debug (every line of external tool output) and Minimal jumped straight to Warning. The two enums
/// were also converted by raw ordinal cast, and the <see cref="Logging.Level" /> getter threw for the
/// Information and Fatal minimums.
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
public class VerbosityLevelMappingSpecs
{
    [Theory]
    [InlineData(Verbosity.Verbose, LogEventLevel.Verbose)]
    [InlineData(Verbosity.Normal, LogEventLevel.Debug)]
    [InlineData(Verbosity.Information, LogEventLevel.Information)]
    [InlineData(Verbosity.Minimal, LogEventLevel.Warning)]
    [InlineData(Verbosity.Quiet, LogEventLevel.Error)]
    public void Each_verbosity_selects_its_serilog_minimum(Verbosity verbosity, LogEventLevel expected)
    {
        WithVerbosity(verbosity, () => Logging.LevelSwitch.MinimumLevel.Should().Be(expected));
    }

    [Fact]
    public void The_information_rung_shows_information_and_hides_debug()
    {
        WithVerbosity(Verbosity.Information, () =>
        {
            var written = Capture();

            written.Should().NotContain(LogEventLevel.Debug);
            written.Should().Contain(LogEventLevel.Information);
            written.Should().Contain(LogEventLevel.Warning);
            written.Should().Contain(LogEventLevel.Error);
        });
    }

    [Fact]
    public void The_normal_rung_still_shows_debug()
    {
        WithVerbosity(Verbosity.Normal, () => Capture().Should().Contain(LogEventLevel.Debug));
    }

    [Theory]
    [InlineData(Verbosity.Verbose)]
    [InlineData(Verbosity.Normal)]
    [InlineData(Verbosity.Information)]
    [InlineData(Verbosity.Minimal)]
    [InlineData(Verbosity.Quiet)]
    public void Verbosity_round_trips_through_the_log_level(Verbosity verbosity)
    {
        WithVerbosity(verbosity, () => FalloutBuild.Verbosity.Should().Be(verbosity));
    }

    [Theory]
    [MemberData(nameof(AllLogEventLevels))]
    public void Every_serilog_level_maps_to_a_defined_log_level(LogEventLevel level)
    {
        var originalVerbosity = FalloutBuild.Verbosity;
        try
        {
            Logging.LevelSwitch.MinimumLevel = level;

            // The getter used to throw for Information and Fatal.
            var mapped = Logging.Level;

            Enum.IsDefined(mapped).Should().BeTrue();
        }
        finally
        {
            FalloutBuild.Verbosity = originalVerbosity;
        }
    }

    [Theory]
    [MemberData(nameof(AllLogEventLevels))]
    public void Every_serilog_level_round_trips_through_the_log_level(LogEventLevel level)
    {
        var originalVerbosity = FalloutBuild.Verbosity;
        try
        {
            Logging.LevelSwitch.MinimumLevel = level;
            var mapped = Logging.Level;

            Logging.Level = mapped;

            Logging.LevelSwitch.MinimumLevel.Should().Be(level);
        }
        finally
        {
            FalloutBuild.Verbosity = originalVerbosity;
        }
    }

    [Fact]
    public void The_conversion_is_not_an_ordinal_cast()
    {
        // LogLevel carries a Critical rung that Verbosity has no member for. An ordinal cast would
        // produce an undefined Verbosity value; the explicit mapping picks the closest rung.
        var originalVerbosity = FalloutBuild.Verbosity;
        try
        {
            Logging.Level = LogLevel.Critical;

            var verbosity = FalloutBuild.Verbosity;

            Enum.IsDefined(verbosity).Should().BeTrue("an ordinal cast would yield (Verbosity)5");
            verbosity.Should().Be(Verbosity.Quiet);
        }
        finally
        {
            FalloutBuild.Verbosity = originalVerbosity;
        }
    }

    [Fact]
    public void Existing_verbosity_members_keep_their_numeric_values()
    {
        // Consumers compile these constants in, so adding a rung must not renumber the others.
        ((int)Verbosity.Verbose).Should().Be(0);
        ((int)Verbosity.Normal).Should().Be(1);
        ((int)Verbosity.Minimal).Should().Be(2);
        ((int)Verbosity.Quiet).Should().Be(3);
    }

    [Fact]
    public void Existing_log_level_members_keep_their_numeric_values()
    {
        ((int)LogLevel.Trace).Should().Be(0);
        ((int)LogLevel.Normal).Should().Be(1);
        ((int)LogLevel.Warning).Should().Be(2);
        ((int)LogLevel.Error).Should().Be(3);
    }

    public static TheoryData<LogEventLevel> AllLogEventLevels
    {
        get
        {
            var data = new TheoryData<LogEventLevel>();
            foreach (var level in Enum.GetValues<LogEventLevel>())
                data.Add(level);
            return data;
        }
    }

    /// <summary>Runs <paramref name="assert" /> at a given verbosity, then restores the previous one.</summary>
    private static void WithVerbosity(Verbosity verbosity, Action assert)
    {
        var original = FalloutBuild.Verbosity;
        try
        {
            FalloutBuild.Verbosity = verbosity;
            assert.Invoke();
        }
        finally
        {
            FalloutBuild.Verbosity = original;
        }
    }

    /// <summary>
    /// Logs one event per level through a logger gated by the shared level switch, and returns the
    /// levels that survived the gate.
    /// </summary>
    private static LogEventLevel[] Capture()
    {
        var sink = new CollectingSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(Logging.LevelSwitch)
            .WriteTo.Sink(sink)
            .CreateLogger();

        foreach (var level in Enum.GetValues<LogEventLevel>())
            logger.Write(level, "a {Level} line", level);

        return sink.Levels.ToArray();
    }

    private class CollectingSink : ILogEventSink
    {
        public List<LogEventLevel> Levels { get; } = new();

        public void Emit(LogEvent logEvent) => Levels.Add(logEvent.Level);
    }
}

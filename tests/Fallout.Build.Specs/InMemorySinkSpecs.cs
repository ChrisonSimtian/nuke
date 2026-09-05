using System;
using Fallout.Common.Execution;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers <see cref="Logging.InMemorySink"/>'s own reset behaviour (FT-1 / #306). Since FT-6 / #311 a
/// sink is per-run state owned by a <see cref="BuildContext"/> rather than a process-wide singleton, so
/// each case exercises its own instance and nothing here needs serialising against other specs. How the
/// per-run sink is resolved and scoped is covered by <c>BuildContextSpecs</c>.
/// </summary>
public class InMemorySinkSpecs
{
    private readonly Logging.InMemorySink sink = new();

    [Fact]
    public void Clear_drops_accumulated_events()
    {
        sink.Emit(CreateLogEvent("first"));
        sink.Emit(CreateLogEvent("second"));
        sink.LogEvents.Should().HaveCount(2);

        sink.Clear();

        sink.LogEvents.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_drops_accumulated_events()
    {
        sink.Emit(CreateLogEvent("only"));
        sink.LogEvents.Should().ContainSingle();

        sink.Dispose();

        sink.LogEvents.Should().BeEmpty();
    }

    [Fact]
    public void Clear_on_an_empty_sink_is_a_no_op()
    {
        sink.LogEvents.Should().BeEmpty();

        sink.Clear();

        sink.LogEvents.Should().BeEmpty();
    }

    [Fact]
    public void A_new_sink_starts_empty()
    {
        // A run's sink is constructed with its context, so a build never inherits earlier events.
        new Logging.InMemorySink().LogEvents.Should().BeEmpty();
    }

    private static LogEvent CreateLogEvent(string message) =>
        new(
            timestamp: DateTimeOffset.UnixEpoch,
            level: LogEventLevel.Warning,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse(message),
            properties: Array.Empty<LogEventProperty>());
}

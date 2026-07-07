using System;
using FluentAssertions;
using Fallout.Common.Execution;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers <see cref="Logging.InMemorySink"/>'s reset added in FT-1 / #306. The sink is a
/// process-wide singleton, so <c>BuildManager.Execute</c>'s <c>finally</c> calls <c>Clear()</c> to
/// stop one run's warnings/errors bleeding into the next build in the same process; <c>Dispose()</c>
/// delegates to the same reset.
/// </summary>
public class InMemorySinkSpecs
{
    private static readonly Logging.InMemorySink Sink = Logging.InMemorySink.Instance;

    public InMemorySinkSpecs()
    {
        // Normalize the shared singleton before each case — prior runs (or a real logging pipeline)
        // may have left events behind.
        Sink.Clear();
    }

    [Fact]
    public void Clear_drops_accumulated_events()
    {
        Sink.Emit(CreateLogEvent("first"));
        Sink.Emit(CreateLogEvent("second"));
        Sink.LogEvents.Should().HaveCount(2);

        Sink.Clear();

        Sink.LogEvents.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_drops_accumulated_events()
    {
        Sink.Emit(CreateLogEvent("only"));
        Sink.LogEvents.Should().ContainSingle();

        Sink.Dispose();

        Sink.LogEvents.Should().BeEmpty();
    }

    [Fact]
    public void Clear_on_an_empty_sink_is_a_no_op()
    {
        Sink.LogEvents.Should().BeEmpty();

        Sink.Clear();

        Sink.LogEvents.Should().BeEmpty();
    }

    private static LogEvent CreateLogEvent(string message) =>
        new(
            timestamp: DateTimeOffset.UnixEpoch,
            level: LogEventLevel.Warning,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse(message),
            properties: Array.Empty<LogEventProperty>());
}

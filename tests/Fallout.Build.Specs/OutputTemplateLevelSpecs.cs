using System;
using System.IO;
using System.Linq;
using Fallout.Common.Execution;
using FluentAssertions;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers the level label in the console output templates. See #570. The templates used
/// <c>{Level:u3}</c>, which renders three-letter codes (<c>DBG</c>, <c>INF</c>, <c>WRN</c>) rather
/// than the level names.
/// </summary>
public class OutputTemplateLevelSpecs
{
    [Theory]
    [InlineData(LogEventLevel.Verbose, "[Verbose]")]
    [InlineData(LogEventLevel.Debug, "[Debug]")]
    [InlineData(LogEventLevel.Information, "[Information]")]
    [InlineData(LogEventLevel.Warning, "[Warning]")]
    [InlineData(LogEventLevel.Error, "[Error]")]
    [InlineData(LogEventLevel.Fatal, "[Fatal]")]
    public void The_standard_template_labels_lines_with_the_level_name(LogEventLevel level, string expected)
    {
        Render(Logging.StandardOutputTemplate, level).Should().StartWith(expected);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, "[Verbose]")]
    [InlineData(LogEventLevel.Debug, "[Debug]")]
    [InlineData(LogEventLevel.Information, "[Information]")]
    [InlineData(LogEventLevel.Warning, "[Warning]")]
    [InlineData(LogEventLevel.Error, "[Error]")]
    [InlineData(LogEventLevel.Fatal, "[Fatal]")]
    public void The_errors_and_warnings_template_labels_lines_with_the_level_name(LogEventLevel level, string expected)
    {
        Render(Logging.ErrorsAndWarningsOutputTemplate, level).Should().StartWith(expected);
    }

    [Theory]
    [InlineData("VRB")]
    [InlineData("DBG")]
    [InlineData("INF")]
    [InlineData("WRN")]
    [InlineData("ERR")]
    [InlineData("FTL")]
    public void No_console_template_renders_a_three_letter_code(string abbreviation)
    {
        var rendered = Enum.GetValues<LogEventLevel>()
            .SelectMany(x => new[]
            {
                Render(Logging.StandardOutputTemplate, x),
                Render(Logging.ErrorsAndWarningsOutputTemplate, x),
                Render(Logging.TimestampOutputTemplate, x)
            });

        rendered.Should().NotContain(x => x.Contains($"[{abbreviation}]"));
    }

    [Fact]
    public void The_timestamped_template_keeps_the_timestamp_ahead_of_the_level()
    {
        Render(Logging.TimestampOutputTemplate, LogEventLevel.Information)
            .Should().MatchRegex(@"^\d{2}:\d{2}:\d{2} \[Information\] ");
    }

    [Fact]
    public void The_message_still_follows_the_level()
    {
        Render(Logging.StandardOutputTemplate, LogEventLevel.Warning).Should().Contain("a warning line");
    }

    [Fact]
    public void The_errors_and_warnings_template_still_names_the_executing_target()
    {
        Render(Logging.ErrorsAndWarningsOutputTemplate, LogEventLevel.Error).Should().Contain("Compile:");
    }

    /// <summary>Renders one event through an output template, the way the console sink does.</summary>
    private static string Render(string outputTemplate, LogEventLevel level)
    {
        var formatter = new MessageTemplateTextFormatter(outputTemplate);
        using var writer = new StringWriter();
        formatter.Format(CreateLogEvent(level), writer);
        return writer.ToString();
    }

    private static LogEvent CreateLogEvent(LogEventLevel level) =>
        new(
            timestamp: DateTimeOffset.UnixEpoch,
            level: level,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse("a warning line"),
            properties: new[]
            {
                new LogEventProperty("ExecutingTarget", new ScalarValue("Compile"))
            });
}

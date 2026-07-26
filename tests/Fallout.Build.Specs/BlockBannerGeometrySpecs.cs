using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fallout.Common.Execution.Theming;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers the geometry of the target block banner drawn by <see cref="Host.WriteBlock"/>. See #552.
/// The two rules were sized by unrelated expressions (<c>text.Length + 5</c> and
/// <c>max(text.Length - 4, 2)</c>), and multi-line text was measured including its newline
/// characters rather than by its widest line.
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
public class BlockBannerGeometrySpecs
{
    [Theory]
    [InlineData("Levels")]
    [InlineData("A")]
    [InlineData("Compile")]
    [InlineData("A considerably longer target name")]
    public void Both_rules_span_the_same_width(string text)
    {
        var (top, _, bottom) = RenderBanner(text);

        top.Length.Should().Be(bottom.Length);
    }

    [Theory]
    [InlineData("Levels")]
    [InlineData("A")]
    [InlineData("A considerably longer target name")]
    public void The_rules_span_the_content_line(string text)
    {
        var (top, content, bottom) = RenderBanner(text);

        top.Should().HaveLength(content.Single().Length);
        bottom.Should().HaveLength(content.Single().Length);
    }

    [Fact]
    public void A_rule_is_a_junction_followed_by_horizontal_bars()
    {
        var (top, _, bottom) = RenderBanner("Compile");

        top.Should().StartWith("╬").And.MatchRegex("^╬═+$");
        bottom.Should().StartWith("╬").And.MatchRegex("^╬═+$");
    }

    [Fact]
    public void Multi_line_text_is_measured_by_its_widest_line()
    {
        var text = string.Join(EnvironmentInfo.NewLine, "short", "the widest line here", "mid");

        var (top, content, bottom) = RenderBanner(text);

        content.Should().HaveCount(3);
        var widest = content.Max(x => x.Length);
        top.Should().HaveLength(widest);
        bottom.Should().HaveLength(widest);
    }

    [Fact]
    public void Multi_line_text_is_not_measured_by_its_raw_length()
    {
        var text = string.Join(EnvironmentInfo.NewLine, "a", "b", "c");

        var (top, _, _) = RenderBanner(text);

        // Raw length is 3 lines plus separators. The widest line is a single character, so a rule
        // sized off the raw string would be visibly wider than the content.
        top.Should().HaveLength("║ a".Length);
    }

    /// <summary>Renders a banner and returns its top rule, content lines, and bottom rule.</summary>
    private static (string Top, string[] Content, string Bottom) RenderBanner(string text)
    {
        var lines = StripAnsi(Capture(() => new BannerHost().WriteBlock(text).Dispose()))
            .Split('\n')
            .Select(x => x.TrimEnd('\r'))
            .Where(x => x.Length > 0)
            .ToArray();

        var top = lines.First();
        var bottom = lines.Last();
        var content = lines.Skip(count: 1).Take(lines.Length - 2).ToArray();
        return (top, content, bottom);
    }

    private static string Capture(Action action)
    {
        var original = Console.Out;
        // WriteBlock writes through the static Host.Debug, which resolves the theme from
        // Host.Instance — and constructing any Host reassigns it. Restore it so this spec doesn't
        // leak a live console-writing host into the rest of the collection.
        var instanceProperty = typeof(Host).GetProperty("Instance", ReflectionUtility.Static)
            .NotNull("typeof(Host).GetProperty(\"Instance\") != null");
        var originalInstance = instanceProperty.GetValue(obj: null);

        using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            action.Invoke();
        }
        finally
        {
            Console.SetOut(original);
            instanceProperty.SetValue(obj: null, originalInstance);
        }

        return writer.ToString();
    }

    /// <summary>
    /// Drops ANSI escape sequences, and the zero-width space the themes emit for a blank line
    /// (#551), so the spec sees only the printable banner and is unaffected by that separate fix.
    /// </summary>
    private static string StripAnsi(string text) =>
        Regex.Replace(text, @"\x1b\[[0-9;]*m", string.Empty).Replace("​", string.Empty);

    /// <summary>Concrete <see cref="Host" /> that keeps the base banner rendering.</summary>
    private class BannerHost : Host
    {
        internal override IHostTheme Theme => AnsiConsoleHostTheme.Default256AnsiColorTheme;
    }
}

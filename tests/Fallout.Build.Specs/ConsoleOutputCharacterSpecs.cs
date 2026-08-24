using System;
using System.IO;
using System.Linq;
using Fallout.Common.Execution.Theming;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Guards normal console output against non-printing characters. See #551. Two hacks put them
/// there: the ANSI theme returned a zero-width space instead of an empty string for blank text, and
/// <see cref="Host.WriteLogo"/> rewrote every space in the logo as a non-breaking space.
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
public class ConsoleOutputCharacterSpecs
{
    private const string ZeroWidthSpace = "​";
    private const string NonBreakingSpace = " ";

    /// <summary>Leading spaces on the logo's tagline lines.</summary>
    private const int TaglineIndent = 21;

    public static TheoryData<string> BlankTexts => new() { null, string.Empty, " ", "   " };

    [Theory]
    [MemberData(nameof(BlankTexts))]
    public void The_ansi_theme_renders_blank_text_as_an_empty_string(string text)
    {
        var theme = (IHostTheme)AnsiConsoleHostTheme.Default256AnsiColorTheme;

        theme.FormatInformation(text).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(BlankTexts))]
    public void The_system_theme_renders_blank_text_as_an_empty_string(string text)
    {
        var theme = (IHostTheme)SystemConsoleHostTheme.DefaultSystemColorTheme;

        theme.FormatInformation(text).Should().BeEmpty();
    }

    [Fact]
    public void The_two_themes_agree_on_blank_text()
    {
        var ansi = (IHostTheme)AnsiConsoleHostTheme.Default256AnsiColorTheme;
        var system = (IHostTheme)SystemConsoleHostTheme.DefaultSystemColorTheme;

        ansi.FormatInformation(text: null).Should().Be(system.FormatInformation(text: null));
    }

    [Fact]
    public void Blank_themed_output_carries_no_zero_width_space()
    {
        var theme = (IHostTheme)AnsiConsoleHostTheme.Default256AnsiColorTheme;

        theme.FormatInformation(text: null).Should().NotContain(ZeroWidthSpace);
    }

    [Fact]
    public void The_logo_uses_ordinary_spaces()
    {
        CaptureLogo().Should().NotContain(NonBreakingSpace);
    }

    [Fact]
    public void The_logo_carries_no_zero_width_space()
    {
        CaptureLogo().Should().NotContain(ZeroWidthSpace);
    }

    [Fact]
    public void The_logo_still_indents_its_tagline()
    {
        var tagline = CaptureLogo().Split('\n').Single(x => x.Contains("survived the NUKE"));

        // Asserted as a run of ordinary spaces rather than a prefix, because the themed line starts
        // with a colour escape. Non-breaking spaces would not match this.
        tagline.Should().Contain(new string(' ', TaglineIndent));
    }

    private static string CaptureLogo()
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        try
        {
            Console.SetOut(writer);
            // Render through the host the spec's module initializer already installed. Constructing
            // one here would reassign the process-wide Host.Instance that WriteLogo resolves its
            // theme from, and leak that host into the rest of the collection.
            FalloutBuild.Host.WriteLogo();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }
}

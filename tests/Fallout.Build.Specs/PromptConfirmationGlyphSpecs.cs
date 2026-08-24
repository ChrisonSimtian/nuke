using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Fallout.Build.Utilities;
using Fallout.Common.Utilities;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

/// <summary>
/// Covers the glyph <see cref="ConsoleUtility" /> prints once a prompt answer is accepted. See #555,
/// where it was <c>¬</c> — the negation sign, used to mean "confirmed".
/// </summary>
[Collection(ProcessGlobalStateCollection.Name)]
public class PromptConfirmationGlyphSpecs
{
    private const string CheckMark = "✓";
    private const string NegationSign = "¬";

    [Fact]
    public void An_accepted_input_is_marked_with_a_check()
    {
        var console = Prompt(() => ConsoleUtility.PromptForInput("Name?", defaultValue: "fallout"));

        console.LastLine.Should().StartWith(CheckMark);
    }

    [Fact]
    public void An_accepted_input_shows_the_value_after_the_check()
    {
        var console = Prompt(() => ConsoleUtility.PromptForInput("Name?", defaultValue: "fallout"));

        console.LastLine.Should().Be($"{CheckMark}  fallout");
    }

    [Fact]
    public void An_accepted_choice_is_marked_with_a_check()
    {
        var console = Prompt(() => ConsoleUtility.PromptForChoice("Pick?", (Value: 1, Description: "first"), (Value: 2, Description: "second")));

        console.LastLine.Should().Be($"{CheckMark}  first");
    }

    [Fact]
    public void No_prompt_output_uses_the_negation_sign()
    {
        var input = Prompt(() => ConsoleUtility.PromptForInput("Name?", defaultValue: "fallout"));
        var choice = Prompt(() => ConsoleUtility.PromptForChoice("Pick?", (Value: 1, Description: "first")));

        input.Lines.Concat(choice.Lines).Should().NotContain(x => x.Contains(NegationSign));
    }

    [Fact]
    public void The_confirmation_glyph_occupies_a_single_column()
    {
        // The prompt writers pad to BufferWidth on raw character count, so a glyph that renders
        // wider than one cell would push the line past the buffer. See #557 for the wider
        // glyph-vocabulary work.
        CheckMark.Should().HaveLength(1);
        char.IsSurrogate(CheckMark[0]).Should().BeFalse();
    }

    /// <summary>Runs a prompt against a fake console that answers by pressing Enter.</summary>
    private static FakeConsole Prompt(Action action)
    {
        var console = new FakeConsole();
        var originalWrapper = ConsoleUtility.ConsoleWrapper;
        var originalInterrupted = ConsoleUtility.IsInterrupted;
        try
        {
            ConsoleUtility.ConsoleWrapper = console;
            ConsoleUtility.IsInterrupted = false;
            action.Invoke();
        }
        finally
        {
            ConsoleUtility.ConsoleWrapper = originalWrapper;
            ConsoleUtility.IsInterrupted = originalInterrupted;
        }

        return console;
    }

    private class FakeConsole : IConsole
    {
        public List<string> Lines { get; } = new();

        /// <summary>The last non-blank line, trimmed of the writers' right padding.</summary>
        public string LastLine => Lines.Last(x => !x.IsNullOrWhiteSpace()).TrimEnd();

        public int BufferWidth => 80;
        public int CursorLeft { get; set; }
        public int CursorTop { get; set; }

        public void Write(string value, Color? color = null)
        {
        }

        public void WriteLine() => Lines.Add(string.Empty);

        public void WriteLine(string value, Color? color = null) => Lines.Add(value);

        // Every prompt loop exits on Enter, so the default value or the first choice is accepted.
        public ConsoleKeyInfo ReadKey(bool intercept) =>
            new('\r', ConsoleKey.Enter, shift: false, alt: false, control: false);
    }
}

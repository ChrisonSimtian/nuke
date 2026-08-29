using System;
using System.Collections.Generic;
using Fallout.Common.Utilities;
using Serilog.Sinks.SystemConsole.Themes;

namespace Fallout.Common.Execution.Theming;

public class AnsiConsoleHostTheme(
    string successCode,
    IReadOnlyDictionary<ConsoleThemeStyle, string> styles)
    : AnsiConsoleTheme(styles), IHostTheme
{
    public static IHostTheme Default256AnsiColorTheme => new AnsiConsoleHostTheme(
        "\u001B[32;1m",
        new Dictionary<ConsoleThemeStyle, string>
        {
            [ConsoleThemeStyle.Text] = "\u001B[39m",
            [ConsoleThemeStyle.SecondaryText] = "\u001B[38;5;247m",
            [ConsoleThemeStyle.TertiaryText] = "\u001B[38;5;247m",
            [ConsoleThemeStyle.Name] = "\u001B[39;1m",
            [ConsoleThemeStyle.Invalid] = "\u001b[48;5;45;1m",
            [ConsoleThemeStyle.Null] = "\u001b[38;5;45;1m",
            [ConsoleThemeStyle.Number] = "\u001b[38;5;45;1m",
            [ConsoleThemeStyle.String] = "\u001b[38;5;45;1m",
            [ConsoleThemeStyle.Boolean] = "\u001b[38;5;45;1m",
            [ConsoleThemeStyle.Scalar] = "\u001b[38;5;45;1m",
            [ConsoleThemeStyle.LevelVerbose] = "\u001B[90;1m",
            [ConsoleThemeStyle.LevelDebug] = "\u001B[39;1m",
            [ConsoleThemeStyle.LevelInformation] = "\u001B[38;5;45;1m",
            [ConsoleThemeStyle.LevelWarning] = "\u001B[38;5;214;1m",
            [ConsoleThemeStyle.LevelError] = "\u001B[38;5;196;1m",
            [ConsoleThemeStyle.LevelFatal] = "\u001B[38;5;231;1m\u001B[48;5;196m"
        });

    private readonly IReadOnlyDictionary<ConsoleThemeStyle, string> styles = styles;
    private const string AnsiStyleReset = "\u001B[0m";

    public void WriteSuccess(string text)
    {
        Write(text, successCode);
    }

    public void WriteVerbose(string text = null)
    {
        Write(text, styles[ConsoleThemeStyle.LevelVerbose]);
    }

    public void WriteDebug(string text)
    {
        Write(text, styles[ConsoleThemeStyle.LevelDebug]);
    }

    public void WriteInformation(string text)
    {
        Write(text, styles[ConsoleThemeStyle.LevelInformation]);
    }

    public void WriteWarning(string text)
    {
        Write(text, styles[ConsoleThemeStyle.LevelWarning]);
    }

    public void WriteError(string text)
    {
        Write(text, styles[ConsoleThemeStyle.LevelError]);
    }

    string IHostTheme.FormatSuccess(string text)
    {
        return Format(text, successCode);
    }

    string IHostTheme.FormatVerbose(string text)
    {
        return Format(text, styles[ConsoleThemeStyle.LevelVerbose]);
    }

    string IHostTheme.FormatDebug(string text)
    {
        return Format(text, styles[ConsoleThemeStyle.LevelDebug]);
    }

    string IHostTheme.FormatInformation(string text)
    {
        return Format(text, styles[ConsoleThemeStyle.LevelInformation]);
    }

    string IHostTheme.FormatWarning(string text)
    {
        return Format(text, styles[ConsoleThemeStyle.LevelWarning]);
    }

    string IHostTheme.FormatError(string text)
    {
        return Format(text, styles[ConsoleThemeStyle.LevelError]);
    }

    private void Write(string text, string code)
    {
        Console.WriteLine(Format(text, code));
    }

    private string Format(string text, string code)
    {
        // TODO: settings for bold/non-bold ?
        return !text.IsNullOrWhiteSpace()
            ? $"{code}{text}{AnsiStyleReset}"
            : string.Empty;
    }
}

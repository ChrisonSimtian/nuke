using System;
using System.Collections.Generic;
using Fallout.Common.Utilities;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Fallout.Common.Execution.Theming;

public class SystemConsoleHostTheme : SystemConsoleTheme, IHostTheme
{
    public static IHostTheme DefaultSystemColorTheme => new SystemConsoleHostTheme(
        new SystemConsoleThemeStyle { Foreground = ConsoleColor.Green },
        new Dictionary<ConsoleThemeStyle, SystemConsoleThemeStyle>
        {
            [ConsoleThemeStyle.Text] = new(),
            [ConsoleThemeStyle.SecondaryText] = new() { Foreground = ConsoleColor.Gray },
            [ConsoleThemeStyle.TertiaryText] = new() { Foreground = ConsoleColor.Gray },
            [ConsoleThemeStyle.Name] = new() { Foreground = ConsoleColor.Blue },
            [ConsoleThemeStyle.Invalid] = new() { Foreground = ConsoleColor.DarkRed },
            [ConsoleThemeStyle.Null] = new() { Foreground = ConsoleColor.Magenta },
            [ConsoleThemeStyle.String] = new() { Foreground = ConsoleColor.Magenta },
            [ConsoleThemeStyle.Number] = new() { Foreground = ConsoleColor.Magenta },
            [ConsoleThemeStyle.Boolean] = new() { Foreground = ConsoleColor.Magenta },
            [ConsoleThemeStyle.Scalar] = new() { Foreground = ConsoleColor.Magenta },
            [ConsoleThemeStyle.LevelVerbose] = new() { Foreground = ConsoleColor.Gray },
            [ConsoleThemeStyle.LevelDebug] = new(),
            [ConsoleThemeStyle.LevelInformation] = new() { Foreground = ConsoleColor.Cyan },
            [ConsoleThemeStyle.LevelWarning] = new() { Foreground = ConsoleColor.Yellow },
            [ConsoleThemeStyle.LevelError] = new() { Foreground = ConsoleColor.Red },
            [ConsoleThemeStyle.LevelFatal] = new() { Foreground = ConsoleColor.White, Background = ConsoleColor.Red }
        });

    private readonly SystemConsoleThemeStyle successStyle;
    private readonly IReadOnlyDictionary<ConsoleThemeStyle, SystemConsoleThemeStyle> styles;

    public SystemConsoleHostTheme(
        SystemConsoleThemeStyle successStyle,
        IReadOnlyDictionary<ConsoleThemeStyle, SystemConsoleThemeStyle> styles)
        : base(styles)
    {
        this.successStyle = successStyle;
        this.styles = styles;
    }

    public void WriteSuccess(string text)
    {
        Write(text, successStyle);
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
        return Format(text);
    }

    string IHostTheme.FormatVerbose(string text)
    {
        return Format(text);
    }

    string IHostTheme.FormatDebug(string text)
    {
        return Format(text);
    }

    string IHostTheme.FormatInformation(string text)
    {
        return Format(text);
    }

    string IHostTheme.FormatWarning(string text)
    {
        return Format(text);
    }

    string IHostTheme.FormatError(string text)
    {
        return Format(text);
    }

    private void Write(string text, SystemConsoleThemeStyle style)
    {
        using (DelegateDisposable.SetAndRestore(() => Console.ForegroundColor, style.Foreground ?? Console.ForegroundColor))
        using (DelegateDisposable.SetAndRestore(() => Console.BackgroundColor, style.Background ?? Console.BackgroundColor))
        {
            Console.WriteLine(Format(text));
        }
    }

    private string Format(string text)
    {
        // This theme carries colour through the console's own foreground/background, so formatting
        // is identity for real text. Blank text still normalises to an empty string, matching
        // AnsiConsoleHostTheme.
        return !text.IsNullOrWhiteSpace() ? text : string.Empty;
    }
}

using System;
using System.Linq;
using Serilog.Events;

namespace Fallout.Common;

/// <summary>
/// Explicit conversions between <see cref="Verbosity"/>, <see cref="LogLevel"/> and Serilog's
/// <see cref="LogEventLevel"/>.
/// </summary>
/// <remarks>
/// These used to be ordinal casts, which coupled three independently-owned enums by declaration
/// order: adding a member to any of them silently re-mapped the others. Keep the conversions
/// exhaustive and explicit. See #556.
/// </remarks>
internal static class LevelMapping
{
    public static LogLevel ToLogLevel(this Verbosity verbosity)
        => verbosity switch
        {
            Verbosity.Verbose => LogLevel.Trace,
            Verbosity.Normal => LogLevel.Normal,
            Verbosity.Information => LogLevel.Information,
            Verbosity.Minimal => LogLevel.Warning,
            Verbosity.Quiet => LogLevel.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, message: null)
        };

    public static Verbosity ToVerbosity(this LogLevel level)
        => level switch
        {
            LogLevel.Trace => Verbosity.Verbose,
            LogLevel.Normal => Verbosity.Normal,
            LogLevel.Information => Verbosity.Information,
            LogLevel.Warning => Verbosity.Minimal,
            LogLevel.Error => Verbosity.Quiet,
            // Verbosity has no Critical rung, and adding one would give users a setting that hides
            // ordinary errors. Quiet is the closest thing a user can ask for.
            LogLevel.Critical => Verbosity.Quiet,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: null)
        };

    public static LogEventLevel ToLogEventLevel(this LogLevel level)
        => level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Normal => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: null)
        };

    public static LogLevel ToLogLevel(this LogEventLevel level)
        => level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Normal,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, message: null)
        };
}

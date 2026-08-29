namespace Fallout.Common;

/// <summary>
/// Minimum severity that reaches the log, in the framework's own vocabulary.
/// </summary>
/// <remarks>
/// Covers the same set of severities as <c>ILogger</c> and Serilog. <see cref="Normal"/> is the
/// historical name for the debug severity. Members are appended rather than ordered by severity,
/// because consumers compile the numeric values in. Ordered least to most severe, the ladder is
/// <see cref="Trace"/>, <see cref="Normal"/>, <see cref="Information"/>, <see cref="Warning"/>,
/// <see cref="Error"/>, <see cref="Critical"/>. Do not renumber — convert explicitly instead.
/// See #556.
/// </remarks>
public enum LogLevel
{
    /// <summary>Trace-level detail.</summary>
    Trace,

    /// <summary>Debug severity, including the output of external tools.</summary>
    Normal,

    /// <summary>Warnings.</summary>
    Warning,

    /// <summary>Errors.</summary>
    Error,

    /// <summary>Informational messages: build steps and tool invocations.</summary>
    Information,

    /// <summary>Unrecoverable failures. Maps to Serilog's fatal severity.</summary>
    Critical
}

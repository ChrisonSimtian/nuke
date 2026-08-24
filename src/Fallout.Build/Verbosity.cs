using System;
using System.Linq;

namespace Fallout.Common;

/// <summary>
/// Logging verbosity of a build run, selected with <c>--verbosity</c>.
/// </summary>
/// <remarks>
/// Members are appended rather than ordered by how much they show, because consumers compile the
/// numeric values in. Ordered quietest to loudest, the ladder is <see cref="Quiet"/>,
/// <see cref="Minimal"/>, <see cref="Information"/>, <see cref="Normal"/>, <see cref="Verbose"/>.
/// Do not renumber — convert explicitly instead. See #556.
/// </remarks>
public enum Verbosity
{
    /// <summary>Everything, including trace-level detail.</summary>
    Verbose,

    /// <summary>Build steps, plus the output of every external tool that runs.</summary>
    Normal,

    /// <summary>Warnings and errors only.</summary>
    Minimal,

    /// <summary>Errors only.</summary>
    Quiet,

    /// <summary>
    /// Build steps and tool invocations, without the tools' own output. Sits between
    /// <see cref="Minimal"/> and <see cref="Normal"/>.
    /// </summary>
    Information
}

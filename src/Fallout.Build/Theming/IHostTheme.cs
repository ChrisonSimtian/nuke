using System;
using System.Linq;

namespace Fallout.Common.Execution.Theming;

public interface IHostTheme
{
    void WriteSuccess(string text = null);
    void WriteVerbose(string text = null);
    void WriteDebug(string text = null);
    void WriteInformation(string text = null);
    void WriteWarning(string text = null);
    void WriteError(string text = null);

    // The Format* methods return an empty string for text that is null, empty, or whitespace only.
    // Implementations must not substitute a filler character for a blank line — see #551, where a
    // zero-width space was used and leaked into every blank line of build output.
    internal string FormatSuccess(string text);
    internal string FormatVerbose(string text);
    internal string FormatDebug(string text);
    internal string FormatInformation(string text);
    internal string FormatWarning(string text);
    internal string FormatError(string text);
}

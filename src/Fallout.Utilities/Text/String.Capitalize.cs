using System;
using System.Diagnostics;
using System.Globalization;
using Humanizer;

namespace Fallout.Common.Utilities;

[DebuggerNonUserCode]
[DebuggerStepThrough]
public static partial class StringExtensions
{
    /// <summary>
    /// Converts the first character of a given string to upper-case.
    /// </summary>
    [Obsolete("Use Humanizer's Transform(To.SentenceCase) instead. " +
              "This forwarder will be removed in a future major.")]
    public static string Capitalize(this string text)
    {
        return text.IsNullOrEmpty()
            ? text
            : text.Transform(CultureInfo.InvariantCulture, To.SentenceCase);
    }
}

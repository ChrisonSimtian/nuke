using System;

namespace Fallout.Common.Tools.CorFlags;

partial class CorFlagsSettings
{
    private static string FormatBoolean(bool? value)
        => value switch
        {
            true => "+",
            false => "-",
            null => null
        };
}

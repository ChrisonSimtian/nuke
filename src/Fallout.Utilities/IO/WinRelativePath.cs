using System;
using static Fallout.Common.IO.PathConstruction;

namespace Fallout.Common.IO;

/// <summary>
/// Represents a relative path with the Windows separator (backward slash).
/// </summary>
[Serializable]
public class WinRelativePath : RelativePath
{
    protected WinRelativePath(string path, char? separator)
        : base(path, separator)
    {
    }

    public static explicit operator WinRelativePath(string path)
    {
        return new WinRelativePath(NormalizePath(path, WinSeparator), WinSeparator);
    }
}

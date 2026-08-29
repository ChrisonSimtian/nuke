using System;
using static Fallout.Common.IO.PathConstruction;

namespace Fallout.Common.IO;

/// <summary>
/// Represents a relative path with the UNIX separator (forward slash).
/// </summary>
[Serializable]
public class UnixRelativePath : RelativePath
{
    protected UnixRelativePath(string path, char? separator)
        : base(path, separator)
    {
    }

    public static explicit operator UnixRelativePath(string path)
    {
        return new UnixRelativePath(NormalizePath(path, UnixSeparator), UnixSeparator);
    }
}

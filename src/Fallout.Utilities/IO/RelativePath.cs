using System;
using System.Diagnostics;
using static Fallout.Common.IO.PathConstruction;

namespace Fallout.Common.IO;

/// <summary>
/// Represents a relative path with the separator of the current operating system.
/// </summary>
[Serializable]
[DebuggerDisplay("{" + nameof(path) + "}")]
public class RelativePath
{
    private readonly string path;
    private readonly char? separator;

    protected RelativePath(string path, char? separator = null)
    {
        this.path = path;
        this.separator = separator;
    }

    public static explicit operator RelativePath(string path)
    {
        if (path is null)
        {
            return null;
        }

        return new RelativePath(NormalizePath(path));
    }

    public static implicit operator string(RelativePath path)
    {
        return path?.path;
    }

#if NET6_0_OR_GREATER

    public static RelativePath operator /(RelativePath left, Range range)
    {
        Assert.True(range.Equals(Range.All));
        return left / "..";
    }

#endif

    public static RelativePath operator /(RelativePath left, string right)
    {
        var separator = left.NotNull().separator;
        return new RelativePath(NormalizePath(Combine(left, (RelativePath)right, separator), separator), separator);
    }

    public static RelativePath operator +(RelativePath left, string right)
    {
        return new RelativePath(left.ToString() + right);
    }

    public override string ToString()
    {
        return path;
    }
}

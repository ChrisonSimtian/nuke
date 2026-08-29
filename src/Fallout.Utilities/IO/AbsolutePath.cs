#if NET6_0_OR_GREATER
using System.Text.Json.Serialization;
using Fallout.Utilities.Converters;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Fallout.Common.Utilities;
using static Fallout.Common.IO.PathConstruction;
using TypeConverter = Fallout.Utilities.Converters.TypeConverter;

namespace Fallout.Common.IO;

/// <summary>
/// Represents an absolute path without distinction between files and directories.
/// </summary>
[Serializable]
#if NET6_0_OR_GREATER
[JsonConverter(typeof(AbsolutePathJsonConverter))]
#endif
[TypeConverter(typeof(TypeConverter))]
[DebuggerDisplay("{" + nameof(path) + "}")]
public class AbsolutePath : IAbsolutePathHolder, IFormattable
{
    public const string DoubleQuote = "d";
    public const string DoubleQuoteIfNeeded = "dn";
    public const string SingleQuote = "s";
    public const string SingleQuoteIfNeeded = "sn";
    public const string NoQuotes = "nq";

    public static AbsolutePath Create(string path)
    {
        return new AbsolutePath(path);
    }

    /// <summary>
    /// Returns a unique path within the OS temp directory, optionally prefixed with <paramref name="prefix"/>.
    /// Does not create the file or directory.
    /// </summary>
    public static AbsolutePath Temp(string prefix = null)
    {
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
        var name = string.IsNullOrEmpty(prefix) ? suffix : $"{prefix}-{suffix}";
        return Create(Path.GetTempPath()) / name;
    }

    private readonly string path;

    private AbsolutePath(string path)
    {
        this.path = NormalizePath(path);
    }

    AbsolutePath IAbsolutePathHolder.Path => this;

    public static implicit operator AbsolutePath(string path)
    {
        if (path is null)
        {
            return null;
        }

        Assert.True(HasPathRoot(path), $"Path '{path}' must be rooted");
        return new AbsolutePath(path);
    }

    public static implicit operator string(AbsolutePath path)
    {
        return path?.ToString();
    }

    /// <summary>
    /// Returns the name of the file or directory.
    /// </summary>
    public string Name => Path.GetFileName(path);

    /// <summary>
    /// Returns the name of the file without extension.
    /// </summary>
    public string NameWithoutExtension => Path.GetFileNameWithoutExtension(path);

    /// <summary>
    /// Returns the extension of the file with dot.
    /// </summary>
    public string Extension => Path.GetExtension(path);

    /// <summary>
    /// Returns the parent path (directory).
    /// </summary>
    public AbsolutePath Parent =>
        !IsWinRoot(path.TrimEnd(WinSeparator)) && !IsUncRoot(path) && !IsUnixRoot(path)
            ? this / ".."
            : null;

#if NET6_0_OR_GREATER

    public static AbsolutePath operator /(AbsolutePath left, Range range)
    {
        Assert.True(range.Equals(Range.All));
        return left.Parent;
    }

#endif

    public static AbsolutePath operator /(AbsolutePath left, string right)
    {
        return new AbsolutePath(Combine(left.NotNull(), right));
    }

    public static AbsolutePath operator +(AbsolutePath left, string right)
    {
        return new AbsolutePath(left.ToString() + right);
    }

    public static bool operator ==(AbsolutePath a, AbsolutePath b)
    {
        return EqualityComparer<AbsolutePath>.Default.Equals(a, b);
    }

    public static bool operator !=(AbsolutePath a, AbsolutePath b)
    {
        return !EqualityComparer<AbsolutePath>.Default.Equals(a, b);
    }

    protected bool Equals(AbsolutePath other)
    {
        var stringComparison = HasWinRoot(path) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(path, other.path, stringComparison);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(objA: null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((AbsolutePath)obj);
    }

    public override int GetHashCode()
    {
        return path?.GetHashCode() ?? 0;
    }

    public override string ToString()
    {
        return ((IFormattable)this).ToString(format: null, formatProvider: null);
    }

    /// <summary>
    /// <para>
    /// Returns a string representation of the path.
    /// </para>
    /// <para>
    /// Available formats are:
    /// <ul>
    /// <li><c>d</c> – <see cref="StringExtensions.DoubleQuote"/></li>
    /// <li><c>dn</c> – <see cref="StringExtensions.DoubleQuoteIfNeeded(string)"/></li>
    /// <li><c>s</c> – <see cref="StringExtensions.SingleQuote"/></li>
    /// <li><c>sn</c> – <see cref="StringExtensions.SingleQuoteIfNeeded(string)"/></li>
    /// <li><c>nq</c> – no quoting</li>
    /// </ul>
    /// </para>
    /// </summary>
    public string ToString(string format)
    {
        return ((IFormattable)this).ToString(format, formatProvider: null);
    }

    string IFormattable.ToString(string format, IFormatProvider formatProvider)
    {
        return format switch
        {
            DoubleQuote => path.DoubleQuote(),
            DoubleQuoteIfNeeded => path.DoubleQuoteIfNeeded(),
            SingleQuote => path.SingleQuote(),
            SingleQuoteIfNeeded => path.SingleQuoteIfNeeded(),
            null or NoQuotes => path,
            _ => throw new ArgumentException($"Format '{format}' is not recognized")
        };
    }
}

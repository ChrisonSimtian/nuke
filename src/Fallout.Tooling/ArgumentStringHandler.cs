#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fallout.Common.IO;
using Fallout.Common.Utilities;

namespace Fallout.Common.Tooling;

[InterpolatedStringHandler]
public ref struct ArgumentStringHandler
{
    private DefaultInterpolatedStringHandler builder;
    private readonly List<string> secretValues;

    public ArgumentStringHandler(
        int literalLength,
        int formattedCount,
        out bool handlerIsValid)
    {
        builder = new(literalLength, formattedCount);
        secretValues = new List<string>();
        handlerIsValid = true;
    }

    public static implicit operator ArgumentStringHandler(string value)
    {
        return $"{value.NotNull()}";
    }

    public void AppendLiteral(string value)
    {
        builder.AppendLiteral(value);
    }

    public void AppendFormatted(object obj, int alignment = 0, string format = null)
    {
        if (obj is string value)
        {
            if (format == "r")
                secretValues.Add(value);
            else if (!(value.IsDoubleQuoted() || value.IsSingleQuoted() || format == "nq"))
                (value, format) = (value.DoubleQuoteIfNeeded(), null);
            AppendFormatted(value, alignment, format);
        }
        else if (obj is IAbsolutePathHolder holder)
            AppendFormatted(holder, alignment, format);
        else
            AppendFormatted(obj.ToString(), alignment, format);
    }

    private void AppendFormatted(string value, int alignment, string format)
    {
        builder.AppendFormatted(value, alignment, format);
    }

    private void AppendFormatted(IAbsolutePathHolder holder, int alignment, string format)
    {
        builder.AppendFormatted(holder.Path, alignment, format ?? AbsolutePath.DoubleQuoteIfNeeded);
    }

    public void AppendFormatted(IEnumerable<IAbsolutePathHolder> paths, int alignment = 0, string format = null)
    {
        var list = paths.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            builder.AppendFormatted(list[i], alignment, format ?? AbsolutePath.DoubleQuoteIfNeeded);
            if (i + 1 < list.Count)
                builder.AppendLiteral(" ");
        }
    }

    public string ToStringAndClear()
    {
        var value = builder.ToStringAndClear();
        return value.Length > 1 &&  value.IndexOf(value: '"', startIndex: 1) == value.Length - 1
            ? value.TrimMatchingDoubleQuotes()
            : value;
    }

    public Func<string, string> GetFilter()
    {
        var secretValues = this.secretValues;
        return x => secretValues.Aggregate(x, (arguments, value) => arguments.Replace(value, "[REDACTED]"));
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fallout.CodeGeneration.Writers;

public static class WriterExtensions
{
    public static TWrapper ForEach<TWrapper, TItem>(
        this TWrapper writerWrapper,
        IEnumerable<TItem> enumerable,
        Action<TWrapper, TItem> action)
        where TWrapper : IWriterWrapper
    {
        foreach (var item in enumerable)
        {
            action(writerWrapper, item);
        }

        return writerWrapper;
    }

    public static TWrapper ForEach<TWrapper, TItem>(
        this TWrapper writerWrapper,
        IEnumerable<TItem> enumerable,
        Action<TItem> action)
        where TWrapper : IWriterWrapper
    {
        foreach (var item in enumerable)
        {
            action(item);
        }

        return writerWrapper;
    }

    public static TWrapper ForEach<TWrapper, TItem>(
        this TWrapper writerWrapper,
        IEnumerable<TItem> enumerable,
        Action<TItem, bool> action)
        where TWrapper : IWriterWrapper
    {
        var list = enumerable.ToList();
        foreach (var item in list)
        {
            action(item, item.Equals(list.Last()));
        }

        return writerWrapper;
    }

    public static T WriteLine<T>(this T writerWrapper, string text)
        where T : IWriterWrapper
    {
        if (text != null)
        {
            writerWrapper.Writer.WriteLine(text.Trim());
        }

        return writerWrapper;
    }

    public static T ForEachWriteLine<T>(this T writerWrapper, IEnumerable<string> texts)
        where T : IWriterWrapper
    {
        return writerWrapper.ForEach(texts, x => writerWrapper.WriteLine(x));
    }

    public static T WriteLineIfTrue<T>(this T writerWrapper, bool condition, string text)
        where T : IWriterWrapper
    {
        if (condition)
        {
            writerWrapper.WriteLine(text);
        }

        return writerWrapper;
    }

    public static T WriteBlock<T>(this T writerWrapper, Action<T> action)
        where T : IWriterWrapper
    {
        writerWrapper.Writer.WriteBlock(() => action(writerWrapper));
        return writerWrapper;
    }

    public static T When<T>(this T writerWrapper, bool condition, Action<T> action)
        where T : IWriterWrapper
    {
        if (condition)
        {
            action(writerWrapper);
        }

        return writerWrapper;
    }
}

using System;
using System.IO;

namespace Fallout.Common.Utilities;

public class CustomFileWriter
{
    private readonly StreamWriter streamWriter;
    private readonly int indentationFactor;
    private readonly string commentPrefix;
    private int indentation;

    public CustomFileWriter(StreamWriter streamWriter, int indentationFactor, string commentPrefix)
    {
        this.streamWriter = streamWriter;
        this.indentationFactor = indentationFactor;
        this.commentPrefix = commentPrefix;
    }

    public void WriteLine(string text = null)
    {
        streamWriter.WriteLine(
            text != null
                ? $"{' '.Repeat(indentation * indentationFactor)}{text}"
                : string.Empty);
    }

    public void WriteComment(string text = null)
    {
        WriteLine($"{commentPrefix} {text}".TrimEnd());
    }

    public void Write(Action<CustomFileWriter> writer)
    {
        writer(this);
    }

    public IDisposable Indent()
    {
        return DelegateDisposable.CreateBracket(
            () => indentation++,
            () => indentation--);
    }
}

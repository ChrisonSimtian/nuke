using System;
using System.IO;
using Fallout.CodeGeneration.Model;
using Fallout.Common.Utilities;

namespace Fallout.CodeGeneration.Writers;

public class ToolWriter : IDisposable, IWriter, IWriterWrapper
{
    private readonly StreamWriter streamWriter;
    private int indention;

    public ToolWriter(Tool tool, StreamWriter streamWriter)
    {
        Tool = tool;
        this.streamWriter = streamWriter;
    }

    public Tool Tool { get; }

    public IWriter Writer => this;

    public void Dispose()
    {
        streamWriter.Dispose();
    }

    void IWriter.WriteLine(string text)
    {
        streamWriter.WriteLine($"{' '.Repeat(indention * 4)}{text}");
    }

    void IWriter.WriteBlock(Action action)
    {
        this.WriteLine("{");
        indention++;
        action();
        indention--;
        this.WriteLine("}");
    }
}

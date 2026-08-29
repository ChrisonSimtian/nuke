using Fallout.CodeGeneration.Model;

namespace Fallout.CodeGeneration.Writers;

public class DataClassWriter : IWriterWrapper
{
    public DataClassWriter(DataClass dataClass, ToolWriter writer)
    {
        DataClass = dataClass;
        Writer = writer;
    }

    public DataClass DataClass { get; }

    public IWriter Writer { get; }
}

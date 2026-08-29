using Fallout.CodeGeneration.Model;

namespace Fallout.CodeGeneration.Writers;

public class TaskWriter : IWriterWrapper
{
    public TaskWriter(Task task, ToolWriter toolWriter)
    {
        Task = task;
        Writer = toolWriter;
    }

    public Task Task { get; }

    public IWriter Writer { get; }
}

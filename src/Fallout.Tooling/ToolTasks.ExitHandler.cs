using System;

namespace Fallout.Common.Tooling;

public abstract partial class ToolTasks
{
    private Action<ToolOptions, IProcess> GetExitHandlerInternal(ToolOptions options = null,
        Func<IProcess, object> exitHandler = null)
    {
        if (options is { ProcessExitHandling: false })
        {
            return (_, _) => { };
        }

        if (exitHandler != null)
        {
            return (_, p) => exitHandler.Invoke(p);
        }

        return options?.ProcessExitHandler != null
            ? (o, p) => options.ProcessExitHandler.Invoke(o, p)
            : (o, p) => GetExitHandler(options).Invoke(o, p);
    }

    protected virtual partial Func<ToolOptions, IProcess, object> GetExitHandler(ToolOptions options)
    {
        return (_, p) => p.AssertZeroExitCode();
    }
}

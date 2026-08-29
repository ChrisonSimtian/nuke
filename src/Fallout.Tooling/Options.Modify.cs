using System;
using System.Text.Json;

namespace Fallout.Common.Tooling;

public static partial class OptionsExtensions
{
    public static T Modify<T>(this T options, Action<IOptions> modification = null)
        where T : IOptions
    {
        var json = JsonSerializer.Serialize(options, options.GetType(), Options.SerializerOptions);
        var copy = (T)JsonSerializer.Deserialize(json, options.GetType(), Options.SerializerOptions);

        // TODO OPTIONS: HACK
        if (options is ToolOptions originalOptions && copy is ToolOptions copiedOptions)
        {
            copiedOptions.ProcessLogger = originalOptions.ProcessLogger;
            copiedOptions.ProcessExitHandler = originalOptions.ProcessExitHandler;
        }

        modification?.Invoke(copy);

        return copy;
    }

    // TODO: only exists for SetProcessExitHandler
    public static T Modify2<T>(this T options, Action<T> modification = null)
        where T : class, IOptions
    {
        return options.Modify(o => modification?.Invoke(o as T));
    }
}

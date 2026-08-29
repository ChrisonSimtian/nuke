using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fallout.Common.Utilities;

[DebuggerNonUserCode]
[DebuggerStepThrough]
public static class TaskExtensions
{
    public static void WaitAll(this IEnumerable<Task> tasks)
    {
        var tasksArray = tasks.ToArray();
        Task.WaitAll(tasksArray);
    }

    public static IReadOnlyCollection<T> WaitAll<T>(this IEnumerable<Task<T>> tasks)
    {
        var tasksArray = tasks.ToArray();
        Task.WaitAll(tasksArray);
        return tasksArray.Select(x => x.Result).ToList();
    }
}

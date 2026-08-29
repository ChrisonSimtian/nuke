using System.Linq;
using Microsoft.Build.Framework;

namespace Fallout.MSBuildTasks;

public static class TaskItemExtensions
{
    public static string GetMetadataOrNull(this ITaskItem taskItem, string metdataName)
    {
        return taskItem.MetadataNames.Cast<string>().Contains(metdataName)
            ? taskItem.GetMetadata(metdataName)
            : null;
    }
}

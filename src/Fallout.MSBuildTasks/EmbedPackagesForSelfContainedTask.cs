using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Fallout.MSBuildTasks.Engine;

namespace Fallout.MSBuildTasks;

public class EmbedPackagesForSelfContainedTask : ContextAwareTask
{
    [Required]
    public string ProjectAssetsFile { get; set; }

    [Required]
    public string TargetFramework { get; set; }

    [Output]
    public ITaskItem[] TargetOutputs { get; set; }

    protected override bool ExecuteInner()
    {
        TargetOutputs = PackageToolingEngine.GetEmbeddablePackageFiles(ProjectAssetsFile)
            .Select(x => (ITaskItem)new TaskItem(x)).ToArray();
        return true;
    }
}

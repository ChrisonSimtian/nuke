using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Fallout.MSBuildTasks.Engine;

namespace Fallout.MSBuildTasks;

public class PackPackageToolsTask : ContextAwareTask
{
    [Required]
    public string ProjectAssetsFile { get; set; }

    [Required]
    public string NuGetPackageRoot { get; set; }

    [Required]
    public string TargetFramework { get; set; }

    [Output]
    public ITaskItem[] TargetOutputs { get; set; }

    protected override bool ExecuteInner()
    {
        TargetOutputs = PackageToolingEngine
            .GetPackageToolFiles(ProjectAssetsFile, NuGetPackageRoot, TargetFramework)
            .Select(x =>
            {
                var item = new TaskItem(x.File);
                item.SetMetadata("BuildAction", x.BuildAction);
                item.SetMetadata("PackagePath", x.PackagePath);
                return (ITaskItem)item;
            }).ToArray();
        return true;
    }
}

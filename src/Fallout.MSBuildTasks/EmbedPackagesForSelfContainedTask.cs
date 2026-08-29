using System.IO;
using System.Linq;
using Fallout.Common.Tooling;
using Fallout.Common.Utilities;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
        var packages = NuGetPackageResolver.GetLocalInstalledPackages(ProjectAssetsFile);
        TargetOutputs = packages
            .Where(x => !x.Id.StartsWithOrdinalIgnoreCase("microsoft.netcore.app.runtime"))
            .Where(x => Directory.GetDirectories(x.Directory, "tools").Any())
            .Select(x => new TaskItem(x.File)).ToArray<ITaskItem>();

        return true;
    }
}

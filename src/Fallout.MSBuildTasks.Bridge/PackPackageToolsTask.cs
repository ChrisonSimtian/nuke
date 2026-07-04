using System.Linq;
using Fallout.MSBuildTasks.Protocol;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fallout.MSBuildTasks;

/// <summary>Full-framework shim for collecting a package's tool files (with pack metadata).</summary>
public sealed class PackPackageToolsTask : WorkerBridgeTask
{
    [Required]
    public string ProjectAssetsFile { get; set; }

    [Required]
    public string NuGetPackageRoot { get; set; }

    [Required]
    public string TargetFramework { get; set; }

    [Output]
    public ITaskItem[] TargetOutputs { get; set; }

    protected override string Verb => WorkerVerbs.PackTools;

    protected override string BuildRequestJson()
        => WorkerJson.Serialize(new PackToolsRequest
        {
            ProjectAssetsFile = ProjectAssetsFile,
            NuGetPackageRoot = NuGetPackageRoot,
            TargetFramework = TargetFramework,
        });

    protected override void ConsumeResponse(WorkerResponse response)
        => TargetOutputs = response.ToolFiles.Select(x =>
        {
            var item = new TaskItem(x.File);
            item.SetMetadata("BuildAction", x.BuildAction);
            item.SetMetadata("PackagePath", x.PackagePath);
            return (ITaskItem)item;
        }).ToArray();
}

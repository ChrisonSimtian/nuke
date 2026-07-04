using System.Linq;
using Fallout.MSBuildTasks.Protocol;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Fallout.MSBuildTasks;

/// <summary>Full-framework shim for resolving packages to embed for self-contained single-file builds.</summary>
public sealed class EmbedPackagesForSelfContainedTask : WorkerBridgeTask
{
    [Required]
    public string ProjectAssetsFile { get; set; }

    [Required]
    public string TargetFramework { get; set; }

    [Output]
    public ITaskItem[] TargetOutputs { get; set; }

    protected override string Verb => WorkerVerbs.EmbedPackages;

    protected override string BuildRequestJson()
        => WorkerJson.Serialize(new EmbedPackagesRequest { ProjectAssetsFile = ProjectAssetsFile });

    protected override void ConsumeResponse(WorkerResponse response)
        => TargetOutputs = response.Files.Select(x => (ITaskItem)new TaskItem(x)).ToArray();
}

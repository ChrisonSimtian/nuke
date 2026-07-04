using System.Linq;
using Fallout.MSBuildTasks.Protocol;
using Microsoft.Build.Framework;

namespace Fallout.MSBuildTasks;

/// <summary>Full-framework shim for the tool-wrapper code generation task.</summary>
public sealed class CodeGenerationTask : WorkerBridgeTask
{
    [Required]
    public ITaskItem[] SpecificationFiles { get; set; }

    [Required]
    public string BaseDirectory { get; set; }

    public bool UseNestedNamespaces { get; set; }

    public string BaseNamespace { get; set; }

    public bool UpdateReferences { get; set; }

    protected override string Verb => WorkerVerbs.Codegen;

    protected override string BuildRequestJson()
        => WorkerJson.Serialize(new CodegenRequest
        {
            SpecificationFiles = SpecificationFiles.Select(x => x.GetMetadata("FullPath")).ToArray(),
            BaseDirectory = BaseDirectory,
            UseNestedNamespaces = UseNestedNamespaces,
            BaseNamespace = BaseNamespace,
            UpdateReferences = UpdateReferences,
        });
}

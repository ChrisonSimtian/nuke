using System.Linq;
using Microsoft.Build.Framework;
using Fallout.MSBuildTasks.Engine;

namespace Fallout.MSBuildTasks;

public class CodeGenerationTask : ContextAwareTask
{
    [Required]
    public ITaskItem[] SpecificationFiles { get; set; }

    [Required]
    public string BaseDirectory { get; set; }

    public bool UseNestedNamespaces { get; set; }

    public string BaseNamespace { get; set; }

    public bool UpdateReferences { get; set; }

    protected override bool ExecuteInner()
    {
        CodeGenerationEngine.Generate(
            SpecificationFiles.Select(x => x.GetMetadata("FullPath")).ToList(),
            BaseDirectory,
            UseNestedNamespaces,
            BaseNamespace,
            UpdateReferences,
            message => LogMessage(message: message));
        return true;
    }
}

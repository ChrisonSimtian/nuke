using System.Linq;
using Fallout.CodeGeneration;
using Fallout.CodeGeneration.Model;
using Fallout.Common.IO;
using Fallout.Common.Utilities.Collections;
using Microsoft.Build.Framework;

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
        var specificationFiles = SpecificationFiles.Select(x => x.GetMetadata("Fullpath")).ToList();

        string GetFilePath(Tool tool)
            => (AbsolutePath)BaseDirectory
               / (UseNestedNamespaces ? tool.Name : ".")
               / tool.DefaultOutputFileName;

        string GetNamespace(Tool tool)
            => !UseNestedNamespaces
                ? BaseNamespace
                : string.IsNullOrEmpty(BaseNamespace)
                    ? tool.Name
                    : $"{BaseNamespace}.{tool.Name}";

        specificationFiles
            .ForEachLazy(x => LogMessage(message: $"Handling {x} ..."))
            .ForEach(x => CodeGenerator.GenerateCode(x, GetFilePath, GetNamespace));

        if (UpdateReferences)
        {
            ReferenceUpdater.UpdateReferences(specificationFiles);
        }

        return true;
    }
}

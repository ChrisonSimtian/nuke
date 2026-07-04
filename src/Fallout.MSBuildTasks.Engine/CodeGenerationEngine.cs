using System;
using System.Collections.Generic;
using Fallout.CodeGeneration;
using Fallout.CodeGeneration.Model;
using Fallout.Common.IO;
using Fallout.Common.Utilities.Collections;

namespace Fallout.MSBuildTasks.Engine;

/// <summary>
/// MSBuild-free tool-wrapper code generation. Lifted out of <c>CodeGenerationTask</c> so the same
/// logic serves both the in-process net10 task and the out-of-process worker.
/// </summary>
public static class CodeGenerationEngine
{
    public static void Generate(
        IReadOnlyList<string> specificationFiles,
        string baseDirectory,
        bool useNestedNamespaces,
        string baseNamespace,
        bool updateReferences,
        Action<string> log)
    {
        string GetFilePath(Tool tool)
            => (AbsolutePath)baseDirectory
               / (useNestedNamespaces ? tool.Name : ".")
               / tool.DefaultOutputFileName;

        string GetNamespace(Tool tool)
            => !useNestedNamespaces
                ? baseNamespace
                : string.IsNullOrEmpty(baseNamespace)
                    ? tool.Name
                    : $"{baseNamespace}.{tool.Name}";

        specificationFiles
            .ForEachLazy(x => log($"Handling {x} ..."))
            .ForEach(x => CodeGenerator.GenerateCode(x, GetFilePath, GetNamespace));

        if (updateReferences)
            ReferenceUpdater.UpdateReferences(specificationFiles);
    }
}

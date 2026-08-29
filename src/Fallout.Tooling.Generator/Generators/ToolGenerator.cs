using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.CodeGeneration.Model;
using Fallout.CodeGeneration.Writers;
using Fallout.Common.Utilities.Collections;

// ReSharper disable UnusedMethodReturnValue.Local

namespace Fallout.CodeGeneration.Generators;

public static class ToolGenerator
{
    public static void Run(Tool tool, StreamWriter streamWriter)
    {
        using var writer = new ToolWriter(tool, streamWriter);
        writer
            // TODO [3]: extract license from dotsettings file
            .WriteLineIfTrue(tool.SourceFile != null, $"// Generated from {tool.SourceFile}")
            .WriteLine(string.Empty)
            .ForEach(GetNamespaceImports(tool), x => writer.WriteLine($"using {x};"))
            .WriteLine(string.Empty)
            .WriteLineIfTrue(tool.Namespace != null, $"namespace {tool.Namespace};")
            .WriteLine(string.Empty)
            .WriteAll();
    }

    private static ToolWriter WriteAll(this ToolWriter w)
    {
        return w
            .WriteAlias()
            .WriteDataClasses()
            .WriteEnumerations();
    }

    private static ToolWriter WriteAlias(this ToolWriter writer)
    {
        TaskGenerator.Run(writer.Tool, writer);
        return writer;
    }

    private static ToolWriter WriteDataClasses(this ToolWriter writer)
    {
        var dataClasses = writer.Tool.Tasks.Select(x => x.SettingsClass).Concat(writer.Tool.DataClasses).ToList();
        dataClasses.ForEach(x => DataClassGenerator.Run(x, writer));
        dataClasses.Where(x => x.ExtensionMethods).ForEach(x => DataClassExtensionGenerator.Run(x, writer));
        return writer;
    }

    private static ToolWriter WriteEnumerations(this ToolWriter writer)
    {
        writer.Tool.Enumerations.ForEach(x => EnumerationGenerator.Run(x, writer));
        return writer;
    }

    private static IEnumerable<string> GetNamespaceImports(Tool tool)
    {
        return new[]
            {
                "Fallout.Common",
                "Fallout.Common.Tooling",
                "Fallout.Common.Tools",
                "Fallout.Common.Utilities.Collections",
                "System",
                "System.Collections.Generic",
                "System.Collections.ObjectModel",
                "System.ComponentModel",
                "System.Diagnostics.CodeAnalysis",
                "System.IO",
                "System.Linq",
                "System.Text",
                "System.Text.Json.Serialization"
            }
            .Concat(tool.Imports ?? new List<string>())
            .OrderBy(x => x);
    }
}

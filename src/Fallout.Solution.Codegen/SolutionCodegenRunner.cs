using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.Solutions;

namespace Fallout.SolutionCodegen;

/// <summary>
/// The codegen pipeline: discover <c>[Solution(GenerateProjects = true)]</c> members in the given
/// sources, resolve each solution file, and (re)write <c>&lt;Member&gt;.g.cs</c> into the output
/// directory via the shared <see cref="SolutionEmitter"/>.
/// </summary>
internal static class SolutionCodegenRunner
{
    /// <param name="rootDirectory">Repo root; relative solution paths resolve against it.</param>
    /// <param name="outDir">Directory the <c>*.g.cs</c> files are written to.</param>
    /// <param name="sources">C# files to scan for the attribute.</param>
    /// <returns>The generated file name and resolved solution file for each emitted member.</returns>
    public static IReadOnlyList<(string FileName, AbsolutePath SolutionFile)> Run(
        AbsolutePath rootDirectory, string outDir, IEnumerable<string> sources)
    {
        Directory.CreateDirectory(outDir);

        // Regenerate from a clean slate: drop any *.g.cs from a previous run so a removed or renamed
        // [Solution] member can't leave an orphan that the build still compiles.
        foreach (var stale in Directory.EnumerateFiles(outDir, "*.g.cs"))
            File.Delete(stale);

        var generated = new List<(string, AbsolutePath)>();
        foreach (var (memberName, relativePath, fancyNames) in SolutionMemberDiscovery.Discover(sources))
        {
            var solutionFile = !string.IsNullOrEmpty(relativePath)
                ? rootDirectory / relativePath
                : GetSolutionFileFromParametersFile(rootDirectory, memberName);

            var solution = solutionFile.ReadSolution();
            var fileName = memberName + ".g.cs";
            File.WriteAllText(Path.Combine(outDir, fileName), SolutionEmitter.Emit(solution, memberName, fancyNames));
            generated.Add((fileName, solutionFile));
        }

        return generated;
    }

    private static AbsolutePath GetSolutionFileFromParametersFile(AbsolutePath rootDirectory, string memberName)
    {
        var parametersFile = Constants.GetDefaultParametersFile(rootDirectory);
        Assert.FileExists(parametersFile);
        var obj = JsonNode.Parse(File.ReadAllText(parametersFile)).NotNull().AsObject();
        var solutionRelativePath = obj[memberName].NotNull($"Property '{memberName}' does not exist in '{parametersFile}'.").GetValue<string>();
        return rootDirectory / solutionRelativePath.NotNull();
    }
}

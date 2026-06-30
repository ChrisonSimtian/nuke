using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Utilities;
using Fallout.SolutionCodegen;
using Fallout.Solutions;

// Pre-build codegen for the strongly-typed [Solution] accessor.
//
// Usage:
//   Fallout.Solution.Codegen --root <rootDir> --out <outDir> [--source <file> ...]
//
// Discovers [Solution(GenerateProjects = true)] members by parsing the given source files
// syntactically (no compilation available pre-build), resolves each solution file, and writes
// <MemberName>.g.cs into <outDir> using the shared SolutionEmitter. No-op (exit 0) if none found.

var root = GetOption("--root") ?? Directory.GetCurrentDirectory();
var outDir = GetOption("--out") ?? Path.Combine(root, "obj");
var sources = GetOptions("--source").Where(File.Exists).ToList();
if (sources.Count == 0)
    sources = SolutionMemberDiscovery.EnumerateProjectSources(root).ToList();

var rootDirectory = (AbsolutePath)root;
Directory.CreateDirectory(outDir);

// Regenerate from a clean slate: drop any *.g.cs from a previous run so a removed or renamed
// [Solution] member can't leave an orphan that the build still compiles.
foreach (var stale in Directory.EnumerateFiles(outDir, "*.g.cs"))
    File.Delete(stale);

foreach (var (memberName, relativePath, fancyNames) in SolutionMemberDiscovery.Discover(sources))
{
    var solutionFile = !string.IsNullOrEmpty(relativePath)
        ? rootDirectory / relativePath
        : GetSolutionFileFromParametersFile(rootDirectory, memberName);

    var solution = solutionFile.ReadSolution();
    File.WriteAllText(Path.Combine(outDir, memberName + ".g.cs"), SolutionEmitter.Emit(solution, memberName, fancyNames));
    Console.WriteLine($"Fallout.Solution.Codegen: generated {memberName}.g.cs from {solutionFile}");
}

return 0;

string GetOption(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

IEnumerable<string> GetOptions(string name)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (args[i] == name)
            yield return args[i + 1];
}

static AbsolutePath GetSolutionFileFromParametersFile(AbsolutePath rootDirectory, string memberName)
{
    var parametersFile = Constants.GetDefaultParametersFile(rootDirectory);
    Assert.FileExists(parametersFile);
    var obj = JsonNode.Parse(File.ReadAllText(parametersFile)).NotNull().AsObject();
    var solutionRelativePath = obj[memberName].NotNull($"Property '{memberName}' does not exist in '{parametersFile}'.").GetValue<string>();
    return rootDirectory / solutionRelativePath.NotNull();
}

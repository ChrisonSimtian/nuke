using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fallout.Common.IO;
using Fallout.SolutionCodegen;

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

foreach (var (fileName, solutionFile) in SolutionCodegenRunner.Run((AbsolutePath)root, outDir, sources))
    Console.WriteLine($"Fallout.Solution.Codegen: generated {fileName} from {solutionFile}");

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

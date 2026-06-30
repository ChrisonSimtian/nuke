using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Fallout.SolutionCodegen;
using Xunit;

namespace Fallout.Solution.Codegen.Specs;

public class SourceEnumerationSpecs : IDisposable
{
    private readonly string _root;

    public SourceEnumerationSpecs()
    {
        _root = Path.Combine(Path.GetTempPath(), "fallout-sources-" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Enumerates_sources_recursively_but_skips_bin_obj_and_hidden_dirs()
    {
        Write("Build.cs");
        Write("src/Project/Component.cs");
        Write("bin/Debug/Generated.cs");
        Write("obj/Debug/Solution.g.cs");
        Write(".git/hooks/Sneaky.cs");
        Write("src/.cache/Cached.cs");

        var found = SolutionMemberDiscovery.EnumerateProjectSources(_root)
            .Select(x => Path.GetFileName(x))
            .ToList();

        found.Should().BeEquivalentTo("Build.cs", "Component.cs");
    }

    private void Write(string relativePath)
    {
        var full = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "// test");
    }
}

using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PublicApiGenerator;
using VerifyXunit;
using Xunit;

namespace Fallout.PublicApi.Specs;

/// <summary>
/// Snapshots the public API surface of each consumer-facing assembly (via PublicApiGenerator +
/// Verify). A change to the surface fails the matching case until its <c>.verified.txt</c> is
/// re-accepted: an accidental break fails CI, and an intentional one shows up as a reviewable
/// snapshot diff — the same gesture the repo already uses for source-generator snapshots.
///
/// This is the leaner alternative to the Roslyn PublicApiAnalyzers guard (#530): one test file
/// plus snapshots under <c>tests/</c>, reusing the mandated xUnit + Verify stack, versus a
/// per-assembly <c>PublicAPI.*.txt</c> in <c>src/</c> with <c>.editorconfig</c>/WarningsAsErrors
/// plumbing.
///
/// Generator-emitted tool wrappers are excluded by their <c>[GeneratedCode]</c> marker (stamped
/// by Fallout.Tooling.Generator): that surface is regenerated from the <c>*.json</c> specs and
/// already guarded by the generator's git-clean check. Because a metadata-based tool can only
/// exclude whole types, a class split across a generated partial and a hand-written partial is
/// dropped entirely — the residual precision gap versus the file-precise Roslyn track.
/// </summary>
public class PublicApiSpecs
{
    [Theory]
    [InlineData("Fallout.Build.Shared")]
    [InlineData("Fallout.Common")]
    [InlineData("Fallout.Components")]
    [InlineData("Fallout.Core")]
    [InlineData("Fallout.ProjectModel")]
    [InlineData("Fallout.Solution")]
    [InlineData("Fallout.Utilities.IO.Compression")]
    [InlineData("Fallout.Utilities.IO.Globbing")]
    [InlineData("Fallout.Utilities.Net")]
    [InlineData("Fallout.Utilities.Text.Json")]
    [InlineData("Fallout.Utilities.Text.Yaml")]
    public Task Public_api_surface(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);

        // Top-level types only — nested types render under their declaring type. Skip anything the
        // tooling generator emitted; that surface has its own guard (see the class remarks).
        var handWrittenTypes = assembly.GetExportedTypes()
            .Where(type => type.DeclaringType == null)
            .Where(type => !type.IsDefined(typeof(GeneratedCodeAttribute), inherit: false))
            .ToArray();

        var publicApi = assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            // Assembly-level attributes (RepositoryUrl, TargetFramework, …) vary by fork and build;
            // excluding them keeps the snapshots portable across clones and CI.
            IncludeAssemblyAttributes = false,
            IncludeTypes = handWrittenTypes,
        });

        return Verifier.Verify(publicApi).UseParameters(assemblyName);
    }
}

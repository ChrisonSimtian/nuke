using System.Threading.Tasks;
using Xunit;

namespace Fallout.Migrate.Analyzers.Specs;

public class NukeMigrationAnalyzerSpecs
{
    // Stubs so the test compilations actually resolve the legacy Nuke.* names.
    // Without these the compiler emits CS0246 alongside FALLOUT004 and the
    // verifier (rightly) complains about an unexpected extra diagnostic.
    private const string LegacyNukeStub = """
        namespace Nuke.Common
        {
            public class AbsolutePath { }
            namespace Tools.DotNet
            {
                public static class DotNetTasks { public static int X() => 0; }
            }
        }
        """;

    [Fact]
    public async Task FlagsUsingNamespaceDirective()
    {
        var source = $$"""
            using {|FALLOUT004:Nuke.Common|};
            namespace X { class C { } }
            {{LegacyNukeStub}}
            """;

        await new AnalyzerSpecs { TestCode = source }.RunAsync();
    }

    [Fact]
    public async Task FlagsUsingStaticDirective()
    {
        var source = $$"""
            using static {|FALLOUT004:Nuke.Common.Tools.DotNet.DotNetTasks|};
            namespace X { class C { } }
            {{LegacyNukeStub}}
            """;

        await new AnalyzerSpecs { TestCode = source }.RunAsync();
    }

    [Fact]
    public async Task FlagsFullyQualifiedTypeReference()
    {
        var source = $$"""
            namespace X
            {
                class C
                {
                    void M()
                    {
                        {|FALLOUT004:Nuke.Common.AbsolutePath|} x = null;
                    }
                }
            }
            {{LegacyNukeStub}}
            """;

        await new AnalyzerSpecs { TestCode = source }.RunAsync();
    }

    [Fact]
    public async Task FlagsBareNukeBuildBaseType()
    {
        const string source = """
            namespace X
            {
                class B : {|FALLOUT004:NukeBuild|} { }
                class NukeBuild { }
            }
            """;

        await new AnalyzerSpecs { TestCode = source }.RunAsync();
    }

    [Fact]
    public async Task FlagsBareINukeBuildBaseInterface()
    {
        const string source = """
            namespace X
            {
                interface IFoo : {|FALLOUT004:INukeBuild|} { }
                interface INukeBuild { }
            }
            """;

        await new AnalyzerSpecs { TestCode = source }.RunAsync();
    }

    [Fact]
    public async Task DoesNotFireWhenProjectDoesNotReferenceFallout()
    {
        // Drop the Fallout marker reference for this test — the analyzer's guard
        // should short-circuit and produce no diagnostics.
        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<
            NukeMigrationAnalyzer,
            Microsoft.CodeAnalysis.Testing.DefaultVerifier>
        {
            TestCode = $$"""
                using Nuke.Common;
                namespace X { class C { } }
                {{LegacyNukeStub}}
                """,
        };

        // No expected diagnostics: guard should suppress.
        await test.RunAsync();
    }

    [Fact]
    public async Task DoesNotFireOnUnrelatedIdentifiers()
    {
        const string source = """
            namespace X
            {
                class C
                {
                    void M()
                    {
                        var x = 1;
                        var y = System.DateTime.Now;
                    }
                }
            }
            """;

        await new AnalyzerSpecs { TestCode = source }.RunAsync();
    }
}

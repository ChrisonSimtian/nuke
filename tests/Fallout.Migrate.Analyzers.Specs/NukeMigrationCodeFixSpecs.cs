using System.Threading.Tasks;
using Xunit;

namespace Fallout.Migrate.Analyzers.Specs;

public class NukeMigrationCodeFixSpecs
{
    // Both legacy and target namespaces need to exist in the test compilation —
    // legacy for the pre-fix source, target for the fixed source — otherwise the
    // verifier sees CS0246 errors alongside FALLOUT004 and fails the assertion.
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

    private const string TargetFalloutStub = """
        namespace Fallout.Common
        {
            public class AbsolutePath { }
            namespace Tools.DotNet
            {
                public static class DotNetTasks { public static int X() => 0; }
            }
        }
        """;

    [Fact]
    public async Task RewritesUsingNamespaceDirective()
    {
        var source = $$"""
            using {|FALLOUT004:Nuke.Common|};
            namespace X { class C { } }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        var fixedSource = $$"""
            using Fallout.Common;
            namespace X { class C { } }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        await new CodeFixSpecs { TestCode = source, FixedCode = fixedSource }.RunAsync();
    }

    [Fact]
    public async Task RewritesUsingStaticDirective()
    {
        var source = $$"""
            using static {|FALLOUT004:Nuke.Common.Tools.DotNet.DotNetTasks|};
            namespace X { class C { } }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        var fixedSource = $$"""
            using static Fallout.Common.Tools.DotNet.DotNetTasks;
            namespace X { class C { } }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        await new CodeFixSpecs { TestCode = source, FixedCode = fixedSource }.RunAsync();
    }

    [Fact]
    public async Task RewritesFullyQualifiedTypeReference()
    {
        var source = $$"""
            namespace X
            {
                class C
                {
                    object M() => typeof({|FALLOUT004:Nuke.Common.AbsolutePath|});
                }
            }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        var fixedSource = $$"""
            namespace X
            {
                class C
                {
                    object M() => typeof(Fallout.Common.AbsolutePath);
                }
            }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        await new CodeFixSpecs { TestCode = source, FixedCode = fixedSource }.RunAsync();
    }

    [Fact]
    public async Task RewritesNukeBuildToFalloutBuild()
    {
        const string source = """
            namespace X
            {
                class B : {|FALLOUT004:NukeBuild|} { }
                class NukeBuild { }
                class FalloutBuild { }
            }
            """;

        const string fixedSource = """
            namespace X
            {
                class B : FalloutBuild { }
                class NukeBuild { }
                class FalloutBuild { }
            }
            """;

        await new CodeFixSpecs { TestCode = source, FixedCode = fixedSource }.RunAsync();
    }

    [Fact]
    public async Task RewritesINukeBuildToIFalloutBuild()
    {
        const string source = """
            namespace X
            {
                interface IFoo : {|FALLOUT004:INukeBuild|} { }
                interface INukeBuild { }
                interface IFalloutBuild { }
            }
            """;

        const string fixedSource = """
            namespace X
            {
                interface IFoo : IFalloutBuild { }
                interface INukeBuild { }
                interface IFalloutBuild { }
            }
            """;

        await new CodeFixSpecs { TestCode = source, FixedCode = fixedSource }.RunAsync();
    }

    [Fact]
    public async Task PreservesLeadingCommentOnUsingDirective()
    {
        var source = $$"""
            // Legacy build, mid-migration.
            using {|FALLOUT004:Nuke.Common|};
            namespace X { class C { } }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        var fixedSource = $$"""
            // Legacy build, mid-migration.
            using Fallout.Common;
            namespace X { class C { } }
            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        await new CodeFixSpecs { TestCode = source, FixedCode = fixedSource }.RunAsync();
    }
}

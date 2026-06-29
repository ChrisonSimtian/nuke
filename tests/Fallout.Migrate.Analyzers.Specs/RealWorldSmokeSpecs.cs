using System.Threading.Tasks;
using Xunit;

namespace Fallout.Migrate.Analyzers.Specs;

// End-to-end smoke test: the snippet below is distilled from a real pre-rename
// version of this repo's own build/Build.cs (commit 6967a094^, before #54
// renamed Nuke.* → Fallout.*). Exercises the analyzer + codefix on the kinds
// of patterns an actual NUKE consumer project contains, not just synthetic
// minimal cases. If this test breaks, real consumer migrations break too.
public class RealWorldSmokeSpecs
{
    // Stand-in declarations so the test compilation can resolve the legacy
    // Nuke.* namespace structure used in pre-rename consumer code.
    private const string LegacyNukeStub = """
        namespace Nuke.Common
        {
            public class NukeBuild { public static int Execute<T>(System.Linq.Expressions.Expression<System.Func<T, object>> defaultTarget) => 0; }
            public class Target { }
            namespace ProjectModel { public class Solution { } }
            namespace Tools.DotNet { public static class DotNetTasks { } }
        }
        namespace Nuke.Components { public interface IPack { } }
        """;

    private const string TargetFalloutStub = """
        namespace Fallout.Common
        {
            public class FalloutBuild { public static int Execute<T>(System.Linq.Expressions.Expression<System.Func<T, object>> defaultTarget) => 0; }
            public class Target { }
            namespace ProjectModel { public class Solution { } }
            namespace Tools.DotNet { public static class DotNetTasks { } }
        }
        namespace Fallout.Components { public interface IPack { } }
        """;

    [Fact]
    public async Task RewritesRealisticPreRenameBuildClass()
    {
        // Patterns covered, all in one file:
        //   - using Nuke.X.Y;
        //   - using static Nuke.X.Y.Z;
        //   - class Build : NukeBuild
        //   - fully-qualified Nuke.Common.ProjectModel.Solution as a type
        //   - bare reference to a Components interface from Nuke.Components
        var source = $$"""
            using {|FALLOUT004:Nuke.Common|};
            using {|FALLOUT004:Nuke.Common.ProjectModel|};
            using {|FALLOUT004:Nuke.Components|};
            using static {|FALLOUT004:Nuke.Common.Tools.DotNet.DotNetTasks|};

            partial class Build : {|FALLOUT004:NukeBuild|}, {|FALLOUT004:Nuke.Components.IPack|}
            {
                public static int Main() => Execute<Build>(x => null);

                {|FALLOUT004:Nuke.Common.ProjectModel.Solution|} _solution;
            }

            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        var fixedSource = $$"""
            using Fallout.Common;
            using Fallout.Common.ProjectModel;
            using Fallout.Components;
            using static Fallout.Common.Tools.DotNet.DotNetTasks;

            partial class Build : FalloutBuild, Fallout.Components.IPack
            {
                public static int Main() => Execute<Build>(x => null);

                Fallout.Common.ProjectModel.Solution _solution;
            }

            {{LegacyNukeStub}}
            {{TargetFalloutStub}}
            """;

        await new CodeFixSpecs { TestCode = source, FixedCode = fixedSource }.RunAsync();
    }
}

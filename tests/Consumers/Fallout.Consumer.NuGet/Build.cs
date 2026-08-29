//
// Fallout consumer against PUBLISHED Fallout.* packages (pinned in the csproj).
// Validates that the most-recent release's surface is intact from a clean
// consumer's perspective.

using Fallout.Common;
using Fallout.Common.ProjectModel;
using Serilog;

internal class Build : FalloutBuild
{
    public static int Main() => Execute<Build>(x => x.Default);

    [Solution]
    private readonly Solution Solution;

    private Target Default => _ => _
        .Executes(() =>
        {
            Log.Information("hello from fallout consumer (pinned nuget 11.0.8)");
            Log.Information("solution name: {Name}", Solution?.Name ?? "<unbound>");
        });
}
